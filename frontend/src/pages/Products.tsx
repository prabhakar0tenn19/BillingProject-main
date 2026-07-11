import React, { useState, useEffect } from 'react';
import {
  Button, Modal, Form, Input, InputNumber, Select, Space,
  Alert, Typography, message, Popconfirm, Upload, Tag, Spin
} from 'antd';
import {
  PlusOutlined, EditOutlined, DeleteOutlined,
  UploadOutlined, LoadingOutlined, FileImageOutlined
} from '@ant-design/icons';
import api from '../api';

const { Title, Paragraph, Text } = Typography;

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
  basePrice?: number; // mapped dynamically in frontend from generic catalog
}

interface Category {
  id: string;
  name: string;
}

const Products: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [imageLoadingMap, setImageLoadingMap] = useState<Record<string, boolean>>({});
  
  const [form] = Form.useForm();

  // Load products AND resolve base prices using first customer's catalog
  const fetchProducts = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // 1. Fetch raw products
      const res = await api.get('/products');
      if (!res.success) return;
      const productsData = res.data;

      // 2. Fetch customers to resolve base prices (via generic customer catalog)
      const custRes = await api.get('/customers');
      let priceMap: Record<string, number> = {};
      
      if (custRes.success && custRes.data.length > 0) {
        const firstCustId = custRes.data[0].id;
        const catalogRes = await api.get(`/customers/${firstCustId}/pricing/bill-ready`);
        if (catalogRes.success) {
          catalogRes.data.forEach((p: any) => {
            priceMap[p.productId] = p.effectivePrice;
          });
        }
      }

      // Merge resolved base price into products state
      const mergedProducts = productsData.map((p: any) => ({
        ...p,
        basePrice: priceMap[p.id] || 0.00
      }));

      setProducts(mergedProducts);
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
        if (res.data.length > 0 && !selectedCategoryId) {
          setSelectedCategoryId(res.data[0].id); // default to first category
        }
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
      basePrice: product.basePrice || 0.00,
    });
    setIsModalOpen(true);
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

  const handleDelete = async (id: string) => {
    try {
      const res = await api.delete(`/products/${id}`);
      if (res.success) {
        message.success('Product removed successfully');
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

  // ─── Adjusters (Plus/Minus Widgets) ──────────────────────────────────────────

  const handleAdjustPrice = async (product: Product, delta: number) => {
    const newPrice = Math.max(0.01, (product.basePrice || 0) + delta);
    try {
      const res = await api.put(`/products/${product.id}`, {
        name: product.name,
        modelNumber: product.modelNumber,
        categoryId: product.categoryId,
        description: product.description,
        stock: product.stock,
        isActive: product.isActive,
        imageUrl: product.imageUrl,
        imagePublicId: product.imagePublicId,
        basePrice: newPrice
      });
      if (res.success) {
        message.success(`Price updated to ₹${newPrice.toFixed(2)}`);
        fetchProducts();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to adjust price');
    }
  };

  const handleAdjustStock = async (product: Product, delta: number) => {
    // Prevent stock from going negative
    if (product.stock + delta < 0) {
      message.warning('Stock level cannot be negative!');
      return;
    }
    try {
      const res = await api.patch(`/products/${product.id}/stock`, {
        quantity: delta,
        operation: 'add'
      });
      if (res.success) {
        message.success(`Stock level adjusted`);
        fetchProducts();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to adjust stock');
    }
  };

  // Filter products by selected category
  const filteredProducts = products.filter(p => p.categoryId === selectedCategoryId && p.isActive);
  const activeCategoryName = categories.find(c => c.id === selectedCategoryId)?.name || 'Products';

  return (
    <div>
      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 24 }} />}

      <div className="catalog-layout">
        {/* Left Column: Categories Sidebar */}
        <aside className="category-sidebar">
          <h4 className="sidebar-title">Categories</h4>
          <div className="category-sidebar-items-wrapper">
            {categories.map(cat => (
              <div
                key={cat.id}
                className={`category-menu-item ${selectedCategoryId === cat.id ? 'active' : ''}`}
                onClick={() => setSelectedCategoryId(cat.id)}
              >
                <span>{cat.name}</span>
              </div>
            ))}
          </div>
        </aside>

        {/* Right Column: Main Products Catalog */}
        <main>
          {/* Main Title Section */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginBottom: 32, flexWrap: 'wrap', gap: 16 }}>
            <div>
              <h1 style={{ margin: 0, fontSize: '38px', lineHeight: 1 }}>{activeCategoryName}</h1>
              <Text style={{ color: '#855b14', fontWeight: 600, fontSize: '14px', marginTop: 8, display: 'block' }}>
                {filteredProducts.length} {filteredProducts.length === 1 ? 'product' : 'products'} cataloged
              </Text>
            </div>
            
            <Space>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={openCreateModal}
                size="large"
                style={{ background: '#855b14', borderColor: '#855b14' }}
              >
                Add Product
              </Button>
            </Space>
          </div>

          {loading ? (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '250px' }}>
              <Spin size="large" tip="Loading catalog products..." />
            </div>
          ) : (
            <>
              {filteredProducts.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '60px 0', background: '#ffffff', borderRadius: 20, border: '1px solid #f1ebd9' }}>
                  <FileImageOutlined style={{ fontSize: 48, color: '#c2bcae', marginBottom: 16 }} />
                  <p style={{ color: '#858076', fontSize: 15 }}>No products registered in this category yet.</p>
                </div>
              ) : (
                <div className="product-grid">
                  {filteredProducts.map(prod => {
                    // Decide stock color indicator
                    let stockColor = '#10b981'; // green
                    if (prod.stock === 0) stockColor = '#ef4444'; // red
                    else if (prod.stock <= 5) stockColor = '#f59e0b'; // orange

                    return (
                      <div className="luxury-product-card" key={prod.id}>
                        {/* Image crop circle */}
                        <div style={{ position: 'relative', width: 140, height: 140, margin: '0 auto 16px' }}>
                          {prod.imageUrl ? (
                            <img
                              src={prod.imageUrl}
                              alt={prod.name}
                              className="product-circle-img"
                            />
                          ) : (
                            <div className="product-circle-img" style={{ background: '#faf9f6', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                              <FileImageOutlined style={{ color: '#c2bcae', fontSize: '32px' }} />
                            </div>
                          )}

                          {/* Float upload overlay */}
                          <Upload
                            accept="image/*"
                            showUploadList={false}
                            beforeUpload={(file) => {
                              handleImageUpload(prod.id, file);
                              return false;
                            }}
                          >
                            <Button
                              shape="circle"
                              icon={imageLoadingMap[prod.id] ? <LoadingOutlined /> : <UploadOutlined />}
                              size="small"
                              style={{ position: 'absolute', bottom: 4, right: 4, background: '#ffffff', border: '1px solid #cbd5e1', boxShadow: '0 2px 4px rgba(0,0,0,0.1)' }}
                              disabled={imageLoadingMap[prod.id]}
                            />
                          </Upload>
                        </div>

                        {/* Title and Model */}
                        <h3 className="product-card-title">{prod.name}</h3>
                        <div className="product-card-model">{prod.modelNumber}</div>

                        {/* Price Adjuster Widget */}
                        <div className="adjuster-widget">
                          <button className="adjuster-btn" onClick={() => handleAdjustPrice(prod, -10)}>-</button>
                          <span className="adjuster-value">₹ {(prod.basePrice || 0).toFixed(2)}</span>
                          <button className="adjuster-btn" onClick={() => handleAdjustPrice(prod, 10)}>+</button>
                        </div>

                        {/* Stock Adjuster Widget */}
                        <div className="adjuster-widget">
                          <button className="adjuster-btn" onClick={() => handleAdjustStock(prod, -1)}>-</button>
                          <span className="adjuster-value">
                            <span className="status-dot" style={{ background: stockColor }} />
                            {prod.stock} stock
                          </span>
                          <button className="adjuster-btn" onClick={() => handleAdjustStock(prod, 1)}>+</button>
                        </div>

                        {/* Card Controls */}
                        <div style={{ marginTop: 16, display: 'flex', justifyContent: 'center', gap: '8px' }}>
                          <Button size="small" icon={<EditOutlined />} onClick={() => openEditModal(prod)}>
                            Edit
                          </Button>
                          <Popconfirm
                            title="Remove product from catalog?"
                            onConfirm={() => handleDelete(prod.id)}
                            okText="Delete"
                            cancelText="Cancel"
                          >
                            <Button size="small" danger icon={<DeleteOutlined />} />
                          </Popconfirm>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </>
          )}
        </main>
      </div>

      {/* Edit/Create Product Modal */}
      <Modal
        title={editingProduct ? 'Modify Product Details' : 'Add New Product'}
        open={isModalOpen}
        onOk={handleModalSubmit}
        onCancel={() => setIsModalOpen(false)}
        className="luxury-modal"
        okText={editingProduct ? 'Save Changes' : 'Add Product'}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Product Name"
            rules={[{ required: true, message: 'Please enter product name' }]}
          >
            <Input placeholder="e.g. Premium Brass Mixer Tap" />
          </Form.Item>

          <Form.Item
            name="modelNumber"
            label="Model Number"
            rules={[{ required: true, message: 'Please enter model number' }]}
          >
            <Input placeholder="e.g. AQUA-201" style={{ textTransform: 'uppercase' }} />
          </Form.Item>

          <Form.Item
            name="categoryId"
            label="Category"
            rules={[{ required: true, message: 'Please select category' }]}
          >
            <Select placeholder="Choose product category">
              {categories.map(cat => (
                <Select.Option key={cat.id} value={cat.id}>{cat.name}</Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item name="description" label="Description / Specification">
            <Input.TextArea rows={2} placeholder="Dimensions, coating finish, connection size, etc." />
          </Form.Item>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <Form.Item
              name="basePrice"
              label="Base Price (₹)"
              rules={[{ required: true, message: 'Please enter base price' }]}
              initialValue={100}
            >
              <InputNumber style={{ width: '100%' }} min={0.01} precision={2} prefix="₹" />
            </Form.Item>

            <Form.Item
              name="stock"
              label="Initial Stock Level"
              rules={[{ required: true, message: 'Please enter initial stock level' }]}
              initialValue={10}
            >
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

export default Products;
