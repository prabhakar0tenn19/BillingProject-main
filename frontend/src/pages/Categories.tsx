import React, { useState, useEffect } from 'react';
import { Table, Button, Modal, Form, Input, Space, Alert, Typography, message, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Paragraph } = Typography;

interface Category {
  id: string;
  name: string;
  description?: string;
  hsnCode: string;
  createdAt: string;
  updatedAt: string;
}

const Categories: React.FC = () => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [form] = Form.useForm();

  const fetchCategories = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await api.get('/categories');
      if (res.success) {
        setCategories(res.data);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch categories');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  const openCreateModal = () => {
    setEditingCategory(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const openEditModal = (category: Category) => {
    setEditingCategory(category);
    form.setFieldsValue({
      name: category.name,
      description: category.description,
      hsnCode: category.hsnCode,
    });
    setIsModalOpen(true);
  };

  const handleModalSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingCategory) {
        // Update category
        const res = await api.put(`/categories/${editingCategory.id}`, values);
        if (res.success) {
          message.success('Category updated successfully');
          fetchCategories();
          setIsModalOpen(false);
        }
      } else {
        // Create category
        const res = await api.post('/categories', values);
        if (res.success) {
          message.success('Category created successfully');
          fetchCategories();
          setIsModalOpen(false);
        }
      }
    } catch (err: any) {
      message.error(err.message || 'Validation failed or API request failed');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await api.delete(`/categories/${id}`);
      if (res.success) {
        message.success('Category soft deleted successfully');
        fetchCategories();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete category');
    }
  };

  const columns = [
    {
      title: 'Category Name',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Typography.Text strong>{text}</Typography.Text>,
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: 'HSN Code',
      dataIndex: 'hsnCode',
      key: 'hsnCode',
      render: (text: string) => <code style={{ background: '#f1f5f9', padding: '2px 6px', borderRadius: '4px' }}>{text}</code>,
    },
    {
      title: 'Last Updated',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      render: (date: string) => dayjs(date).format('DD MMM YYYY, hh:mm A'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (record: Category) => (
        <Space size="middle">
          <Button icon={<EditOutlined />} onClick={() => openEditModal(record)}>
            Edit
          </Button>
          <Popconfirm
            title="Are you sure to delete this category?"
            description="Products linked to this category will remain, but category will be hidden."
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
          <Title level={3} style={{ margin: 0 }}>Product Categories</Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            Expandable list of HSN-coded categories. Invoices automatically split tax based on category HSN.
          </Paragraph>
        </div>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal} size="large">
          Add Category
        </Button>
      </div>

      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 16 }} />}

      <Table
        dataSource={categories}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={{ pageSize: 10 }}
        size="middle"
      />

      <Modal
        title={editingCategory ? 'Edit Category' : 'Create Category'}
        open={isModalOpen}
        onOk={handleModalSubmit}
        onCancel={() => setIsModalOpen(false)}
        okText={editingCategory ? 'Update' : 'Create'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Category Name"
            rules={[
              { required: true, message: 'Please enter category name' },
              { whitespace: true, message: 'Name cannot be empty' }
            ]}
          >
            <Input placeholder="e.g. Taps, Showers, Washbasins" />
          </Form.Item>
          <Form.Item
            name="hsnCode"
            label="HSN Code"
            rules={[
              { required: true, message: 'Please enter GST HSN Code' },
              { pattern: /^[0-9]{4,8}$/, message: 'HSN code must be 4 to 8 digit number' }
            ]}
          >
            <Input placeholder="e.g. 8481 (Taps/Showers), 7325 (Drain Covers)" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={3} placeholder="Brief summary of products in this category" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Categories;
