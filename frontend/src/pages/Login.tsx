import React, { useState } from 'react';
import { Form, Input, Button, Typography, Alert, message, Checkbox, Popover, Modal } from 'antd';
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import api from '../api';
import { setAuth, isAuthenticated } from '../utils/auth';

const { Text } = Typography;

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
    <div className="login-page-container">
      {/* Decorative luxury gold ambient background blurs */}
      <div style={{
        position: 'absolute',
        top: '-10%',
        right: '-10%',
        width: '500px',
        height: '500px',
        borderRadius: '50%',
        background: 'rgba(133, 91, 20, 0.04)',
        filter: 'blur(100px)',
        zIndex: 0
      }} />
      <div style={{
        position: 'absolute',
        bottom: '-10%',
        left: '-10%',
        width: '500px',
        height: '500px',
        borderRadius: '50%',
        background: 'rgba(133, 91, 20, 0.03)',
        filter: 'blur(100px)',
        zIndex: 0
      }} />

      <div className="login-luxury-card" style={{ zIndex: 1 }}>
        {/* Left Column: Gold Brand Panel */}
        <div className="login-brand-panel">
          {/* Logo block */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '28px', color: '#ffffff', display: 'flex', alignItems: 'center' }}>
              <svg width="28" height="28" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" fill="#ffffff" />
              </svg>
            </span>
            <span style={{ fontFamily: 'Playfair Display, Georgia, serif', fontSize: '28px', fontWeight: 800, letterSpacing: '2px', color: '#ffffff' }}>AQUA</span>
          </div>

          {/* Welcome Message */}
          <div style={{ margin: '40px 0' }}>
            <h2 style={{ fontFamily: 'Playfair Display, Georgia, serif', fontSize: '32px', color: '#ffffff', margin: '0 0 16px', fontWeight: 700 }}>
              Welcome Back
            </h2>
            <p style={{ color: 'rgba(255, 255, 255, 0.8)', fontSize: '15px', lineHeight: '1.6', margin: 0, fontWeight: 400 }}>
              Access your Aqua dashboard to manage catalogs, invoices, customers, reports and orders from one place.
            </p>
          </div>

          {/* Glass Capsule */}
          <div className="login-glass-capsule">
            <div style={{
              width: '40px',
              height: '40px',
              borderRadius: '8px',
              background: 'rgba(255, 255, 255, 0.1)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#ffffff',
              fontSize: '18px'
            }}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2m0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 002 2h2a2 2 0 002-2" />
              </svg>
            </div>
            <div style={{ flex: 1, color: 'rgba(255, 255, 255, 0.9)', fontSize: '13px', lineHeight: '1.4', fontWeight: 500 }}>
              Premium inventory and business management platform.
            </div>
          </div>
        </div>

        {/* Right Column: Sign In Form Panel */}
        <div className="login-form-panel">
          <h1 style={{ fontFamily: 'Playfair Display, Georgia, serif', fontSize: '36px', color: '#1e293b', margin: '0 0 8px', fontWeight: 700 }}>
            Sign In
          </h1>
          <p style={{ color: '#858076', fontSize: '14px', margin: '0 0 32px' }}>
            Enter your credentials to continue.
          </p>

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
              label={<Text style={{ fontSize: 13, fontWeight: 600, color: '#1e293b' }}>Username</Text>}
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
              label={<Text style={{ fontSize: 13, fontWeight: 600, color: '#1e293b' }}>Password</Text>}
              rules={[{ required: true, message: 'Please enter your password' }]}
            >
              <Input.Password
                prefix={<LockOutlined style={{ color: '#94a3b8' }} />}
                placeholder="Enter password"
                size="large"
                style={{ height: 44, borderRadius: 8 }}
              />
            </Form.Item>

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
              <Form.Item name="remember" valuePropName="checked" noStyle initialValue={true}>
                <Checkbox style={{ fontSize: '13px', color: '#64748b' }}>Remember me</Checkbox>
              </Form.Item>
              <span
                style={{ fontSize: '13px', color: '#855b14', fontWeight: 600, cursor: 'pointer' }}
                onClick={() => Modal.info({
                  title: 'Reset Password',
                  content: 'Please contact the system administrator to reset or retrieve your password details.'
                })}
              >
                Forgot Password?
              </span>
            </div>

            <Form.Item style={{ marginBottom: '24px' }}>
              <Button
                type="primary"
                htmlType="submit"
                size="large"
                block
                loading={loading}
                style={{
                  height: '46px',
                  borderRadius: '23px',
                  background: '#855b14',
                  borderColor: '#855b14',
                  fontWeight: 600,
                  fontSize: '15px'
                }}
              >
                Sign In
              </Button>
            </Form.Item>
          </Form>

          <div style={{ display: 'flex', alignItems: 'center', margin: '0 0 24px' }}>
            <div style={{ flex: 1, height: '1px', background: '#f1ebd9' }}></div>
            <div style={{ padding: '0 12px', fontSize: '12px', color: '#cbd5e1', textTransform: 'uppercase', letterSpacing: '1px' }}>or</div>
            <div style={{ flex: 1, height: '1px', background: '#f1ebd9' }}></div>
          </div>

          <div style={{ textAlign: 'center', fontSize: '13px', color: '#64748b' }}>
            Need access?{' '}
            <Popover
              content={
                <div style={{ padding: '4px' }}>
                  <Text strong style={{ display: 'block', marginBottom: '4px' }}>System Administrator Support</Text>
                  <Text type="secondary" style={{ fontSize: '12px', display: 'block', margin: '2px 0' }}>📞 Phone: +91 98765 43210</Text>
                  <Text type="secondary" style={{ fontSize: '12px', display: 'block', margin: '2px 0' }}>✉️ Email: admin@aquasanitary.com</Text>
                  <Text type="secondary" style={{ fontSize: '11px', display: 'block', marginTop: '6px', color: '#855b14' }}>Default Admin: admin / Admin@123</Text>
                </div>
              }
              title="Admin Contact Info"
              trigger="click"
            >
              <span style={{ color: '#855b14', fontWeight: 600, cursor: 'pointer', textDecoration: 'underline' }}>
                Contact Administrator
              </span>
            </Popover>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
