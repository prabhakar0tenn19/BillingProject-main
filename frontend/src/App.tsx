import React, { useState, useEffect } from 'react';
import { Routes, Route, Link, useLocation } from 'react-router-dom';
import { Layout, Menu, Button, Space, Typography } from 'antd';
import {
  DashboardOutlined,
  FileTextOutlined,
  PlusCircleOutlined,
  TeamOutlined,
  ShoppingOutlined,
  AppstoreOutlined,
  BarChartOutlined,
  SettingOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';

// Pages imports
import Dashboard from './pages/Dashboard';
import Invoices from './pages/Invoices';
import InvoiceDetails from './pages/InvoiceDetails';
import NewBill from './pages/NewBill';
import Customers from './pages/Customers';
import Products from './pages/Products';
import Categories from './pages/Categories';
import Reports from './pages/Reports';
import Settings from './pages/Settings';

const { Header, Content, Sider } = Layout;
const { Text } = Typography;

const App: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [currentTime, setCurrentTime] = useState(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
  const location = useLocation();

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const menuItems = [
    {
      key: '/',
      icon: <DashboardOutlined />,
      label: <Link to="/">Dashboard</Link>,
    },
    {
      key: '/invoices',
      icon: <FileTextOutlined />,
      label: <Link to="/invoices">Invoices</Link>,
    },
    {
      key: '/new-bill',
      icon: <PlusCircleOutlined />,
      label: <Link to="/new-bill">New Bill</Link>,
    },
    {
      key: '/parties',
      icon: <TeamOutlined />,
      label: <Link to="/parties">Parties (Customers)</Link>,
    },
    {
      key: '/products',
      icon: <ShoppingOutlined />,
      label: <Link to="/products">Product Catalog</Link>,
    },
    {
      key: '/categories',
      icon: <AppstoreOutlined />,
      label: <Link to="/categories">Categories</Link>,
    },
    {
      key: '/reports',
      icon: <BarChartOutlined />,
      label: <Link to="/reports">Reports</Link>,
    },
    {
      key: '/settings',
      icon: <SettingOutlined />,
      label: <Link to="/settings">Company Settings</Link>,
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={(value) => setCollapsed(value)}
        theme="dark"
        width={250}
        className="no-print"
      >
        <div className="brand-section">
          <h2 className="brand-title">{collapsed ? 'BS' : 'Sanitaryware Billing'}</h2>
          {!collapsed && <p className="brand-subtitle">Company Admin Panel</p>}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          style={{ borderRight: 0, marginTop: 16 }}
        />
      </Sider>
      <Layout>
        <Header
          className="no-print"
          style={{
            background: '#ffffff',
            padding: '0 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #f0f0f0',
            height: 64,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center' }}>
            <Typography.Title level={4} style={{ margin: 0, fontWeight: 600 }}>
              {menuItems.find((item) => item.key === location.pathname)?.label?.props.children ||
                (location.pathname.startsWith('/invoices/') ? 'Invoice Detail' : 'Billing System')}
            </Typography.Title>
          </div>
          <Space size="middle">
            <ClockCircleOutlined style={{ color: '#8c8c8c' }} />
            <Text type="secondary" strong>
              {currentTime}
            </Text>
          </Space>
        </Header>
        <Content style={{ margin: '24px 24px 0', overflow: 'initial', display: 'flex', flexDirection: 'column' }}>
          <div
            style={{
              padding: 24,
              background: '#ffffff',
              borderRadius: 10,
              minHeight: 360,
              boxShadow: '0 2px 8px rgba(0,0,0,0.04)',
              flex: 1,
            }}
          >
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/invoices" element={<Invoices />} />
              <Route path="/invoices/:id" element={<InvoiceDetails />} />
              <Route path="/new-bill" element={<NewBill />} />
              <Route path="/parties" element={<Customers />} />
              <Route path="/products" element={<Products />} />
              <Route path="/categories" element={<Categories />} />
              <Route path="/reports" element={<Reports />} />
              <Route path="/settings" element={<Settings />} />
            </Routes>
          </div>
          <Layout.Footer className="no-print" style={{ textAlign: 'center', color: '#8c8c8c', padding: '16px 24px' }}>
            Sanitaryware Manufacturing Billing System &copy; {dayjs().format('YYYY')} — Secure GST B2B Ledger
          </Layout.Footer>
        </Content>
      </Layout>
    </Layout>
  );
};

export default App;
