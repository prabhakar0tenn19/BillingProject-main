import React, { useState } from 'react';
import { Card, Form, Input, Button, Typography, Alert, message } from 'antd';
import { LockOutlined, UserOutlined, ArrowRightOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import api from '../api';
import { setAuth, isAuthenticated } from '../utils/auth';

const { Title, Text, Paragraph } = Typography;

const Login: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  // If already authenticated, redirect to home immediately
  React.useEffect(() => {
    if (isAuthenticated()) {
      navigate('/');
    }
  }, [navigate]);

  const onFinish = async (values: any) => {
    try {
      setLoading(true);
      setError(null);

      const res = await api.post('/auth/login', {
        username: values.username,
        password: values.password,
      });

      if (res.success) {
        setAuth(res.data.token, {
          username: res.data.username,
          fullName: res.data.fullName,
          role: res.data.role,
        });
        message.success('Welcome back to AQUA Billing System!');
        navigate('/');
      } else {
        setError(res.message || 'Login failed. Please check your credentials.');
      }
    } catch (err: any) {
      setError(err.message || 'An error occurred during authentication.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: '#faf9f6', // AQUA brand cream background
      padding: '24px',
      position: 'relative',
      overflow: 'hidden'
    }}>
      {/* Decorative luxury gold ambient background blurs */}
      <div style={{
        position: 'absolute',
        top: '-15%',
        right: '-10%',
        width: '450px',
        height: '450px',
        borderRadius: '50%',
        background: 'rgba(133, 91, 20, 0.04)',
        filter: 'blur(80px)',
        zIndex: 0
      }} />
      <div style={{
        position: 'absolute',
        bottom: '-15%',
        left: '-10%',
        width: '450px',
        height: '450px',
        borderRadius: '50%',
        background: 'rgba(133, 91, 20, 0.03)',
        filter: 'blur(80px)',
        zIndex: 0
      }} />

      <Card style={{
        width: '100%',
        maxWidth: 420,
        borderRadius: 16,
        border: '1px solid #e2e8f0',
        boxShadow: '0 10px 30px rgba(133, 91, 20, 0.05)',
        background: 'rgba(255, 255, 255, 0.95)',
        backdropFilter: 'blur(10px)',
        zIndex: 1,
        padding: '16px 8px'
      }}>
        {/* Luxury Brand Header */}
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <div style={{
            fontSize: '36px',
            color: '#855b14',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: 12
          }}>
            <svg width="40" height="40" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" />
            </svg>
          </div>
          <Title level={2} style={{ margin: 0, fontFamily: 'serif', letterSpacing: '4px', color: '#1e293b', fontWeight: 600 }}>
            AQUA
          </Title>
          <Text type="secondary" style={{ fontSize: 13, letterSpacing: '1px', textTransform: 'uppercase', display: 'block', marginTop: 4 }}>
            B2B GST Ledger Portal
          </Text>
        </div>

        {error && (
          <Alert
            message={error}
            type="error"
            showIcon
            style={{ marginBottom: 24, borderRadius: 8 }}
          />
        )}

        <Form
          name="login_form"
          layout="vertical"
          onFinish={onFinish}
          requiredMark={false}
        >
          <Form.Item
            name="username"
            label={<Text style={{ fontSize: 13, fontWeight: 500, color: '#475569' }}>Username</Text>}
            rules={[{ required: true, message: 'Please enter your username' }]}
          >
            <Input
              prefix={<UserOutlined style={{ color: '#94a3b8' }} />}
              placeholder="Enter username"
              size="large"
              style={{ height: 44, borderRadius: 8 }}
            />
          </Form.Item>

          <Form.Item
            name="password"
            label={<Text style={{ fontSize: 13, fontWeight: 500, color: '#475569' }}>Password</Text>}
            rules={[{ required: true, message: 'Please enter your password' }]}
          >
            <Input.Password
              prefix={<LockOutlined style={{ color: '#94a3b8' }} />}
              placeholder="Enter password"
              size="large"
              style={{ height: 44, borderRadius: 8 }}
            />
          </Form.Item>

          <Form.Item style={{ marginTop: 28, marginBottom: 16 }}>
            <Button
              type="primary"
              htmlType="submit"
              size="large"
              block
              loading={loading}
              icon={<ArrowRightOutlined />}
              style={{
                height: 46,
                borderRadius: 8,
                background: 'var(--primary-color)',
                borderColor: 'var(--primary-color)',
                fontWeight: 600,
                fontSize: 15
              }}
            >
              Sign In to System
            </Button>
          </Form.Item>
        </Form>

        {/* Info card for default credentials */}
        <div style={{
          background: '#f8fafc',
          border: '1px solid #e2e8f0',
          borderRadius: 8,
          padding: '12px 16px',
          marginTop: 24,
          fontSize: 12
        }}>
          <Text strong style={{ color: '#64748b', display: 'block', marginBottom: 4 }}>Default Credentials:</Text>
          <div style={{ display: 'grid', gridTemplateColumns: '80px 1fr', gap: '4px' }}>
            <Text type="secondary">Username:</Text>
            <Text strong style={{ color: '#475569' }}>admin</Text>
            <Text type="secondary">Password:</Text>
            <Text strong style={{ color: '#475569' }}>Admin@123</Text>
          </div>
        </div>
      </Card>
    </div>
  );
};

export default Login;
