import React, { useState, useEffect } from 'react';
import { Card, Form, Input, Button, Upload, Divider, Typography, Space, Alert, message, InputNumber, Badge } from 'antd';
import { UploadOutlined, LoadingOutlined, FileImageOutlined, CheckCircleOutlined, DeleteOutlined } from '@ant-design/icons';
import api from '../api';

const { Title, Paragraph, Text } = Typography;

interface SettingsState {
  id: string;
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  gstin: string;
  bankName: string;
  bankAccount: string;
  bankIfsc: string;
  signatureCloudinaryUrl?: string;
  hasSignature: boolean;
  invoicePrefix: string;
  gstRate: number;
}

const Settings: React.FC = () => {
  const [settings, setSettings] = useState<SettingsState | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [form] = Form.useForm();

  const fetchSettings = async () => {
    try {
      setLoading(true);
      const res = await api.get('/settings');
      if (res.success) {
        setSettings(res.data);
        form.setFieldsValue(res.data);
      }
    } catch (err: any) {
      message.error('Failed to load company configuration: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
  }, []);

  const handleSaveSettings = async () => {
    try {
      setSaving(true);
      const values = await form.validateFields();
      const res = await api.put('/settings', values);
      if (res.success) {
        message.success('Company settings saved successfully');
        fetchSettings();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to update settings');
    } finally {
      setSaving(false);
    }
  };

  const handleSignatureUpload = async (file: File) => {
    const allowedTypes = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      message.error('Only PNG, JPG, and WEBP formats are supported for signature stamp!');
      return false;
    }

    if (file.size > 5 * 1024 * 1024) {
      message.error('Image size must be smaller than 5MB!');
      return false;
    }

    const formData = new FormData();
    formData.append('file', file);

    try {
      setUploading(true);
      const res = await api.post('/settings/signature', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      if (res.success) {
        message.success('Signature uploaded and linked successfully!');
        fetchSettings();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to upload signature');
    } finally {
      setUploading(false);
    }
    return false;
  };

  const handleSignatureDelete = async () => {
    try {
      setUploading(true);
      const res = await api.delete('/settings/signature');
      if (res.success) {
        message.success('Signature removed successfully');
        fetchSettings();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to delete signature');
    } finally {
      setUploading(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '300px' }}>
        <LoadingOutlined style={{ fontSize: 36 }} spin />
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 850, margin: '0 auto' }}>
      <Title level={3}>Company Profile & GST Configuration</Title>
      <Paragraph type="secondary" style={{ marginBottom: 24 }}>
        Configure the official information printed on invoices (legal name, bank details, tax GSTIN, and stamp signature).
      </Paragraph>

      <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 24 }}>
        {/* Settings Form */}
        <Card title="Company Metadata" style={{ borderRadius: 10 }}>
          <Form form={form} layout="vertical" onFinish={handleSaveSettings}>
            <Form.Item
              name="companyName"
              label="Legal Company Name"
              rules={[{ required: true, message: 'Please enter company name' }]}
            >
              <Input placeholder="e.g. Precision Sanitaryware Mfg." />
            </Form.Item>

            <Form.Item
              name="companyAddress"
              label="Official Address"
              rules={[{ required: true, message: 'Please enter company address' }]}
            >
              <Input.TextArea rows={2} placeholder="Complete manufacturing unit / office address" />
            </Form.Item>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <Form.Item
                name="companyPhone"
                label="Phone Number"
                rules={[{ required: true, message: 'Please enter phone' }]}
              >
                <Input placeholder="e.g. +91 98765 43210" />
              </Form.Item>
              <Form.Item name="companyEmail" label="Email Address">
                <Input placeholder="e.g. billing@precisionsanitary.com" />
              </Form.Item>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 16 }}>
              <Form.Item
                name="gstin"
                label="Company GSTIN"
                rules={[{ required: true, message: 'Please enter GSTIN' }]}
              >
                <Input placeholder="e.g. 07AAAAA1111A1Z0" style={{ textTransform: 'uppercase' }} />
              </Form.Item>
              <Form.Item
                name="gstRate"
                label="System GST Rate (%)"
                rules={[{ required: true }]}
              >
                <InputNumber style={{ width: '100%' }} min={0} max={100} suffix="%" />
              </Form.Item>
            </div>

            <Divider orientation={"left" as any}>Bank Settlement Details</Divider>
            <Form.Item
              name="bankName"
              label="Bank Name"
              rules={[{ required: true, message: 'Please enter bank name' }]}
            >
              <Input placeholder="e.g. HDFC Bank Ltd" />
            </Form.Item>

            <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 16 }}>
              <Form.Item
                name="bankAccount"
                label="Account Number"
                rules={[{ required: true, message: 'Please enter account number' }]}
              >
                <Input placeholder="e.g. 501002222333" />
              </Form.Item>
              <Form.Item
                name="bankIfsc"
                label="IFSC Code"
                rules={[{ required: true, message: 'Please enter IFSC' }]}
              >
                <Input placeholder="e.g. HDFC0001234" style={{ textTransform: 'uppercase' }} />
              </Form.Item>
            </div>

            <Divider orientation={"left" as any}>Invoicing Formatting</Divider>
            <Form.Item
              name="invoicePrefix"
              label="Invoice Number Prefix"
              rules={[{ required: true, message: 'Please enter invoice prefix' }]}
            >
              <Input placeholder="e.g. PSM" style={{ textTransform: 'uppercase' }} />
            </Form.Item>

            <Button type="primary" htmlType="submit" size="large" block loading={saving}>
              Save Company Settings
            </Button>
          </Form>
        </Card>

        {/* Digital Signature Card */}
        <Card title="Digital Stamp / Signature" style={{ borderRadius: 10 }}>
          <Paragraph type="secondary" style={{ fontSize: 13 }}>
            Upload your official company seal or authorized signature. This image is fetched dynamically and embedded inside every generated PDF invoice.
          </Paragraph>

          <div style={{ textAlign: 'center', padding: '20px 0' }}>
            {settings?.signatureCloudinaryUrl ? (
              <div style={{ background: '#f8fafc', padding: 16, borderRadius: 8, border: '1px solid #e2e8f0', marginBottom: 16 }}>
                <img
                  src={settings.signatureCloudinaryUrl}
                  alt="Company Signature"
                  style={{ maxHeight: 110, maxWidth: '100%', objectFit: 'contain' }}
                />
                <div style={{ marginTop: 12 }}>
                  <Badge status="success" text="Authorized Stamp Active" />
                </div>
              </div>
            ) : (
              <div style={{ height: 140, background: '#f1f5f9', border: '2px dashed #cbd5e1', borderRadius: 8, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', marginBottom: 16 }}>
                <FileImageOutlined style={{ fontSize: 32, color: '#94a3b8', marginBottom: 8 }} />
                <Text type="secondary" style={{ fontSize: 12 }}>No Stamp Configured</Text>
              </div>
            )}

            <Space direction="vertical" style={{ width: '100%' }}>
              <Upload
                accept="image/*"
                showUploadList={false}
                beforeUpload={handleSignatureUpload}
              >
                <Button icon={uploading ? <LoadingOutlined /> : <UploadOutlined />} type="dashed" block disabled={uploading}>
                  {settings?.signatureCloudinaryUrl ? 'Replace Stamp Image' : 'Upload Stamp Image'}
                </Button>
              </Upload>

              {settings?.signatureCloudinaryUrl && (
                <Button
                  danger
                  icon={<DeleteOutlined />}
                  onClick={handleSignatureDelete}
                  loading={uploading}
                  block
                >
                  Remove Signature Stamp
                </Button>
              )}
            </Space>
          </div>
        </Card>
      </div>
    </div>
  );
};

export default Settings;
