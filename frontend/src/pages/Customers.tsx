import React, { useState, useEffect } from 'react';
import {
  Table, Button, Modal, Form, Input, Space, Alert, Typography,
  message, Popconfirm, Tag, Card, Divider, InputNumber, Drawer, Select
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SettingOutlined, PercentageOutlined } from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Paragraph, Text } = Typography;

interface Customer {
  id: string;
  partyName: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  billingAddress?: string;
  shippingAddress?: string;
  gstin?: string;
  panNumber?: string;
}

interface PriceOverride {
  id?: string;
  productId: string;
  productName: string;
  modelNumber: string;
  negotiatedPrice: number;
}

interface ProductInfo {
  id: string;
  name: string;
  modelNumber: string;
}

const Customers: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Customer Add/Edit Modal
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [form] = Form.useForm();

  // Pricing Catalog Drawer
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [pricingList, setPricingList] = useState<PriceOverride[]>([]);
  const [pricingLoading, setPricingLoading] = useState(false);
  const [allProducts, setAllProducts] = useState<ProductInfo[]>([]);

  // Add pricing override state
  const [isOverrideModalOpen, setIsOverrideModalOpen] = useState(false);
  const [overrideForm] = Form.useForm();

  const fetchCustomers = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await api.get('/customers');
      if (res.success) {
        setCustomers(res.data);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load customers list');
    } finally {
      setLoading(false);
    }
  };

  const fetchAllProducts = async () => {
    try {
      const res = await api.get('/products');
      if (res.success) {
        setAllProducts(res.data.map((p: any) => ({
          id: p.id,
          name: p.name,
          modelNumber: p.modelNumber
        })));
      }
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    fetchCustomers();
    fetchAllProducts();
  }, []);

  const openCreateModal = () => {
    setEditingCustomer(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const openEditModal = (customer: Customer) => {
    setEditingCustomer(customer);
    form.setFieldsValue(customer);
    setIsModalOpen(true);
  };

  const handleModalSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingCustomer) {
        const res = await api.put(`/customers/${editingCustomer.id}`, {
          ...values,
          isActive: true
        });
        if (res.success) {
          message.success('Customer profile updated');
          fetchCustomers();
          setIsModalOpen(false);
        }
      } else {
        const res = await api.post('/customers', values);
        if (res.success) {
          message.success('New customer profile created');
          fetchCustomers();
          setIsModalOpen(false);
        }
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to save customer');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await api.delete(`/customers/${id}`);
      if (res.success) {
        message.success('Customer soft deleted');
        fetchCustomers();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete customer');
    }
  };

  // Pricing Catalog methods
  const openPricingDrawer = async (customer: Customer) => {
    setSelectedCustomer(customer);
    setDrawerVisible(true);
    fetchCustomerPricing(customer.id);
  };

  const fetchCustomerPricing = async (customerId: string) => {
    try {
      setPricingLoading(true);
      const res = await api.get(`/customers/${customerId}/pricing`);
      if (res.success) {
        setPricingList(res.data);
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to load custom catalog');
    } finally {
      setPricingLoading(false);
    }
  };

  const openAddOverrideModal = () => {
    overrideForm.resetFields();
    setIsOverrideModalOpen(true);
  };

  const handleOverrideSubmit = async () => {
    try {
      if (!selectedCustomer) return;
      const values = await overrideForm.validateFields();
      const res = await api.post(`/customers/${selectedCustomer.id}/pricing`, values);
      if (res.success) {
        message.success('Custom catalog price configured');
        fetchCustomerPricing(selectedCustomer.id);
        setIsOverrideModalOpen(false);
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to set pricing');
    }
  };

  const handleDeleteOverride = async (productId: string) => {
    try {
      if (!selectedCustomer) return;
      const res = await api.delete(`/customers/${selectedCustomer.id}/pricing/${productId}`);
      if (res.success) {
        message.success('Override price removed (reverted to base price)');
        fetchCustomerPricing(selectedCustomer.id);
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete price override');
    }
  };

  const columns = [
    {
      title: 'Party / Business Name',
      dataIndex: 'partyName',
      key: 'partyName',
      render: (text: string, record: Customer) => (
        <div>
          <Text strong style={{ fontSize: '15px' }}>{text}</Text>
          {record.gstin && (
            <div style={{ marginTop: 2 }}>
              <Tag color="purple">GSTIN: {record.gstin}</Tag>
            </div>
          )}
        </div>
      ),
    },
    {
      title: 'Contact Details',
      key: 'contact',
      render: (record: Customer) => (
        <div>
          <div><Text strong style={{ fontSize: '12px' }}>{record.contactPerson || 'N/A'}</Text></div>
          <Text type="secondary" style={{ fontSize: '12px' }}>{record.phone || 'No phone'} | {record.email || 'No email'}</Text>
        </div>
      ),
    },
    {
      title: 'Billing Address',
      dataIndex: 'billingAddress',
      key: 'billingAddress',
      ellipsis: true,
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (record: Customer) => (
        <Space size="middle">
          <Button type="primary" ghost icon={<PercentageOutlined />} onClick={() => openPricingDrawer(record)}>
            Override Pricing
          </Button>
          <Button icon={<EditOutlined />} onClick={() => openEditModal(record)}>
            Edit
          </Button>
          <Popconfirm
            title="Are you sure to delete this customer?"
            onConfirm={() => handleDelete(record.id)}
            okText="Yes"
            cancelText="No"
          >
            <Button danger icon={<DeleteOutlined />}>
              Delete
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'between', alignItems: 'center', marginBottom: 20 }}>
        <div style={{ flex: 1 }}>
          <Title level={3} style={{ margin: 0 }}>Parties Directory (Customers)</Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            Configure client details. You can manage negotiated price overrides per customer to automatically load custom catalogs in the billing wizard.
          </Paragraph>
        </div>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal} size="large">
          Create Party
        </Button>
      </div>

      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 16 }} />}

      <Table
        dataSource={customers}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 10 }}
        size="middle"
      />

      {/* Customer Add/Edit Modal */}
      <Modal
        title={editingCustomer ? 'Edit Customer Info' : 'Create Customer Info'}
        open={isModalOpen}
        onOk={handleModalSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText={editingCustomer ? 'Update' : 'Create'}
        width={720}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <Form.Item
              name="partyName"
              label="Business / Party Name"
              rules={[{ required: true, message: 'Please enter company name' }]}
            >
              <Input placeholder="e.g. Agarwal Sanitary Store" />
            </Form.Item>
            <Form.Item name="contactPerson" label="Contact Person Name">
              <Input placeholder="e.g. Ramesh Agarwal" />
            </Form.Item>
            <Form.Item name="phone" label="Phone Number">
              <Input placeholder="e.g. +91 99999 88888" />
            </Form.Item>
            <Form.Item name="email" label="Email Address">
              <Input placeholder="e.g. agarwalsanitary@gmail.com" />
            </Form.Item>
            <Form.Item name="gstin" label="GSTIN (Goods and Services Tax Number)">
              <Input placeholder="e.g. 07AAAAA1111A1Z0" />
            </Form.Item>
            <Form.Item name="panNumber" label="PAN Card Number">
              <Input placeholder="e.g. ABCDE1234F" />
            </Form.Item>
          </div>
          <Form.Item name="billingAddress" label="Billing Address (Printed on Invoices)">
            <Input.TextArea rows={2} placeholder="Complete physical address of headquarters" />
          </Form.Item>
          <Form.Item name="shippingAddress" label="Shipping Address (Delivery Location)">
            <Input.TextArea rows={2} placeholder="Leave blank if identical to billing address" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Pricing Override Catalog Drawer */}
      <Drawer
        title={`Custom Pricing Catalog: ${selectedCustomer?.partyName}`}
        placement="right"
        width={650}
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
        destroyOnClose
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={openAddOverrideModal}>
            Configure Specific Price
          </Button>
        }
      >
        <Table
          dataSource={pricingList}
          rowKey="productId"
          loading={pricingLoading}
          pagination={false}
          columns={[
            {
              title: 'Product Name',
              dataIndex: 'productName',
              key: 'productName',
              render: (text: string, r: PriceOverride) => (
                <div>
                  <Text strong>{text}</Text>
                  <div style={{ fontSize: '11px', color: '#64748b' }}>Model: {r.modelNumber}</div>
                </div>
              ),
            },
            {
              title: 'Custom Negotiated Rate',
              dataIndex: 'negotiatedPrice',
              key: 'negotiatedPrice',
              render: (price: number) => <Text strong style={{ color: '#10b981' }}>₹{price.toFixed(2)}</Text>,
            },
            {
              title: 'Actions',
              key: 'actions',
              width: 100,
              render: (r: PriceOverride) => (
                <Popconfirm
                  title="Remove override price?"
                  description="Pricing for this item will fall back to base default price."
                  onConfirm={() => handleDeleteOverride(r.productId)}
                  okText="Yes"
                  cancelText="No"
                >
                  <Button danger type="link" icon={<DeleteOutlined />}>
                    Remove
                  </Button>
                </Popconfirm>
              ),
            },
          ]}
        />
      </Drawer>

      {/* Set Override Modal */}
      <Modal
        title="Set Negotiated Price"
        open={isOverrideModalOpen}
        onOk={handleOverrideSubmit}
        onCancel={() => setIsOverrideModalOpen(false)}
        destroyOnClose
      >
        <Form form={overrideForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="productId"
            label="Select Product"
            rules={[{ required: true, message: 'Please select a product' }]}
          >
            <Select showSearch optionFilterProp="children" placeholder="Search product catalog">
              {allProducts.map(p => (
                <Select.Option key={p.id} value={p.id}>{p.name} ({p.modelNumber})</Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="negotiatedPrice"
            label="Negotiated Price (Exclusive of GST)"
            rules={[{ required: true, message: 'Please enter override price' }]}
          >
            <InputNumber
              style={{ width: '100%' }}
              min={0.01}
              precision={2}
              formatter={value => `₹ ${value}`}
              placeholder="e.g. 380.00"
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Customers;
