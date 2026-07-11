import React, { useState, useEffect } from 'react';
import {
  Table, Button, Modal, Form, Input, InputNumber, Select, Space,
  Alert, Typography, message, Popconfirm, Upload, Tag, Badge, Image
} from 'antd';
import {
  PlusOutlined, EditOutlined, DeleteOutlined, InboxOutlined,
  UploadOutlined, LoadingOutlined, FileImageOutlined
} from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Paragraph, Text } = Typography;
const { Option } = Select;

interface Product {
  id: string;
  name: string;
  modelNumber: string;
  categoryId: string;
  categoryName: string;
  hsnCode: string;
  description?: string;
  stock: number;
  isActive: boolean;
  imageUrl?: string;
  imagePublicId?: string;
}

interface Category {
  id: string;
  name: string;
}

const Products: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isStockModalOpen, setIsStockModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [stockProduct, setStockProduct] = useState<Product | null>(null);
  const [imageLoadingMap, setImageLoadingMap] = useState<Record<string, boolean>>({});
  
  const [form] = Form.useForm();
  const [stockForm] = Form.useForm();

  const fetchProducts = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await api.get('/products');
      if (res.success) {
        setProducts(res.data);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch product catalog');
    } finally {
      setLoading(false);
    }
  };

  const fetchCategories = async () => {
    try {
      const res = await api.get('/categories');
      if (res.success) {
        setCategories(res.data);
      }
    } catch (err) {
      console.error('Failed to load categories', err);
    }
  };

  useEffect(() => {
    fetchProducts();
    fetchCategories();
  }, []);

  const openCreateModal = () => {
    setEditingProduct(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const openEditModal = (product: Product) => {
    setEditingProduct(product);
    form.setFieldsValue({
      name: product.name,
      modelNumber: product.modelNumber,
      categoryId: product.categoryId,
      description: product.description,
      stock: product.stock,
      basePrice: 100.0, // Default placeholder since basePrice is hidden in public views
    });
    setIsModalOpen(true);
  };

  const openStockModal = (product: Product) => {
    setStockProduct(product);
    stockForm.resetFields();
    setIsStockModalOpen(true);
  };

  const handleModalSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingProduct) {
        const res = await api.put(`/products/${editingProduct.id}`, {
          ...values,
          isActive: editingProduct.isActive,
          imageUrl: editingProduct.imageUrl,
          imagePublicId: editingProduct.imagePublicId
        });
        if (res.success) {
          message.success('Product updated successfully');
          fetchProducts();
          setIsModalOpen(false);
        }
      } else {
        const res = await api.post('/products', values);
        if (res.success) {
          message.success('Product created successfully');
          fetchProducts();
          setIsModalOpen(false);
        }
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to save product');
    }
  };

  const handleStockSubmit = async () => {
    try {
      if (!stockProduct) return;
      const values = await stockForm.validateFields();
      const res = await api.patch(`/products/${stockProduct.id}/stock`, values);
      if (res.success) {
        message.success('Stock level updated successfully');
        fetchProducts();
        setIsStockModalOpen(false);
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to update stock');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await api.delete(`/products/${id}`);
      if (res.success) {
        message.success('Product soft deleted');
        fetchProducts();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete product');
    }
  };

  const handleImageUpload = async (productId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    
    setImageLoadingMap(prev => ({ ...prev, [productId]: true }));
    try {
      const res = await api.post(`/products/${productId}/image`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      if (res.success) {
        message.success('Product image uploaded successfully');
        fetchProducts();
      }
    } catch (err: any) {
      message.error(err.message || 'Image upload failed');
    } finally {
      setImageLoadingMap(prev => ({ ...prev, [productId]: false }));
    }
  };

  const handleImageDelete = async (productId: string) => {
    setImageLoadingMap(prev => ({ ...prev, [productId]: true }));
    try {
      const res = await api.delete(`/products/${productId}/image`);
      if (res.success) {
        message.success('Product image removed');
        fetchProducts();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete image');
    } finally {
      setImageLoadingMap(prev => ({ ...prev, [productId]: false }));
    }
  };

  const columns = [
    {
      title: 'Image',
      dataIndex: 'imageUrl',
      key: 'image',
      width: 80,
      render: (url: string, record: Product) => (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          {url ? (
            <Image
              src={url}
              alt={record.name}
              width={50}
              height={50}
              style={{ objectFit: 'cover', borderRadius: '4px' }}
            />
          ) : (
            <div style={{ width: 50, height: 50, background: '#f1f5f9', display: 'flex', alignItems: 'center', justifyContent: 'center', borderRadius: '4px', border: '1px dashed #cbd5e1' }}>
              <FileImageOutlined style={{ color: '#94a3b8', fontSize: '20px' }} />
            </div>
          )}
          
          <Upload
            accept="image/*"
            showUploadList={false}
            beforeUpload={(file) => {
              handleImageUpload(record.id, file);
              return false;
            }}
          >
            <Button
              type="link"
              size="small"
              icon={imageLoadingMap[record.id] ? <LoadingOutlined /> : <UploadOutlined />}
              style={{ fontSize: '10px', height: 'auto', padding: '4px 0 0 0' }}
            >
              {url ? 'Change' : 'Upload'}
            </Button>
          </Upload>
          {url && (
            <Button
              type="link"
              danger
              size="small"
              onClick={() => handleImageDelete(record.id)}
              style={{ fontSize: '10px', height: 'auto', padding: 0 }}
            >
              Remove
            </Button>
          )}
        </div>
      ),
    },
    {
      title: 'Product Details',
      key: 'details',
      render: (record: Product) => (
        <div>
          <Typography.Text strong style={{ fontSize: '15px' }}>{record.name}</Typography.Text>
          <div style={{ marginTop: 2 }}>
            <Tag color="blue">Model: {record.modelNumber}</Tag>
            <Tag color="cyan">HSN: {record.hsnCode}</Tag>
          </div>
          {record.description && (
            <Text type="secondary" style={{ fontSize: '12px', display: 'block', marginTop: 4 }}>
              {record.description}
            </Text>
          )}
        </div>
      ),
    },
    {
      title: 'Category',
      dataIndex: 'categoryName',
      key: 'categoryName',
      render: (text: string) => <Tag color="geekblue">{text}</Tag>,
    },
    {
      title: 'Stock Status',
      dataIndex: 'stock',
      key: 'stock',
      render: (stock: number, record: Product) => (
        <Space direction="vertical" size={2}>
          <Badge
            status={stock <= 5 ? 'error' : 'success'}
            text={`${stock} units in stock`}
          />
          <Button size="small" type="dashed" onClick={() => openStockModal(record)}>
            Adjust Stock
          </Button>
        </Space>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (record: Product) => (
        <Space size="middle">
          <Button icon={<EditOutlined />} onClick={() => openEditModal(record)}>
            Edit
          </Button>
          <Popconfirm
            title="Are you sure to delete this product?"
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
          <Title level={3} style={{ margin: 0 }}>Product Catalog (Master DB)</Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            Master list of company inventory items. Base price is kept internal and falls back to customer invoices if no specific catalog override exists.
          </Paragraph>
        </div>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal} size="large">
          Create Product
        </Button>
      </div>

      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 16 }} />}

      <Table
        dataSource={products}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 8 }}
        size="middle"
      />

      {/* Create/Edit Modal */}
      <Modal
        title={editingProduct ? 'Edit Product' : 'Create Product'}
        open={isModalOpen}
        onOk={handleModalSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText={editingProduct ? 'Update' : 'Create'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Product Name"
            rules={[{ required: true, message: 'Please enter product name' }]}
          >
            <Input placeholder="e.g. Health Faucet Premium" />
          </Form.Item>
          <Form.Item
            name="modelNumber"
            label="Model Number / SKU"
            rules={[{ required: true, message: 'Please enter model/SKU code' }]}
          >
            <Input placeholder="e.g. HF-009" />
          </Form.Item>
          <Form.Item
            name="categoryId"
            label="Category"
            rules={[{ required: true, message: 'Please select a category' }]}
          >
            <Select placeholder="Choose product category">
              {categories.map(c => (
                <Option key={c.id} value={c.id}>{c.name}</Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="basePrice"
            label="Base Price (Company Reference Price — Hidden from Public)"
            rules={[{ required: true, message: 'Please enter base price' }]}
          >
            <InputNumber
              style={{ width: '100%' }}
              min={0.01}
              precision={2}
              prefix="₹"
              placeholder="e.g. 450.00"
            />
          </Form.Item>
          <Form.Item
            name="stock"
            label="Initial Stock Quantity"
            rules={[{ required: true, message: 'Please enter initial stock level' }]}
            initialValue={0}
          >
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="description" label="Description / Specification">
            <Input.TextArea rows={3} placeholder="Dimensions, metal material coating description" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Adjust Stock Modal */}
      <Modal
        title="Adjust Inventory Levels"
        open={isStockModalOpen}
        onOk={handleStockSubmit}
        onCancel={() => setIsStockModalOpen(false)}
        destroyOnClose
      >
        {stockProduct && (
          <div style={{ marginBottom: 16 }}>
            <Text type="secondary">Product: </Text>
            <Text strong>{stockProduct.name} ({stockProduct.modelNumber})</Text>
            <br />
            <Text type="secondary">Current Level: </Text>
            <Tag color="blue">{stockProduct.stock} units</Tag>
          </div>
        )}
        <Form form={stockForm} layout="vertical">
          <Form.Item
            name="operation"
            label="Adjustment Type"
            initialValue="add"
            rules={[{ required: true }]}
          >
            <Select>
              <Option value="add">Add / Deduct (Relative)</Option>
              <Option value="set">Set Level Directly (Absolute)</Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="quantity"
            label="Quantity (Enter negative number to deduct)"
            rules={[{ required: true, message: 'Please enter adjustment quantity' }]}
          >
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Products;
