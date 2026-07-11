import React, { useState, useEffect } from 'react';
import { Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import { Layout, Button, Space, Typography, Drawer, message } from 'antd';
import {
  DashboardOutlined,
  FileTextOutlined,
  TeamOutlined,
  ShoppingOutlined,
  SettingOutlined,
  ClockCircleOutlined,
  PlusOutlined,
  LogoutOutlined,
  MenuOutlined,
  AppstoreOutlined,
  BarChartOutlined
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
import Login from './pages/Login';

// Auth utilities
import { isAuthenticated, getUser, clearAuth } from './utils/auth';

const { Content } = Layout;
const { Text } = Typography;

const App: React.FC = () => {
  const [currentTime, setCurrentTime] = useState(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
  const [drawerVisible, setDrawerVisible] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

  const authenticated = isAuthenticated();

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  // Redirect to login if unauthenticated and not on the login page
  useEffect(() => {
    if (!authenticated && location.pathname !== '/login') {
      navigate('/login');
    }
  }, [authenticated, location.pathname, navigate]);

  const handleLogout = () => {
    clearAuth();
    message.success('Logged out successfully');
    navigate('/login');
  };

  const isActive = (path: string) => {
    if (path === '/' && location.pathname === '/') return true;
    if (path !== '/' && location.pathname.startsWith(path)) return true;
    return false;
  };

  // If path is /login, render Login component raw without application layout
  if (location.pathname === '/login') {
    return (
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Login />} />
      </Routes>
    );
  }

  return (
    <Layout style={{ minHeight: '100vh', background: '#faf9f6' }}>
      {/* AQUA Top Navigation Header */}
      <header className="header-nav no-print">
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {/* Hamburger Menu Toggle on Mobile */}
          {authenticated && (
            <Button
              type="text"
              icon={<MenuOutlined style={{ fontSize: '20px', color: '#855b14' }} />}
              onClick={() => setDrawerVisible(true)}
              className="mobile-drawer-toggle"
              style={{ padding: 0, height: '40px', width: '40px', display: 'none' }}
            />
          )}

          <div className="logo-container" onClick={() => navigate('/')}>
            {/* Gold building/water drop icon */}
            <span style={{ fontSize: '24px', color: '#855b14', display: 'flex', alignItems: 'center' }}>
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" />
              </svg>
            </span>
            <span className="logo-text">AQUA</span>
          </div>
        </div>

        <nav className="nav-links">
          <Link to="/" className={`nav-item ${isActive('/') ? 'active' : ''}`}>
            Dashboard
          </Link>
          <Link to="/products" className={`nav-item ${isActive('/products') ? 'active' : ''}`}>
            Catalog
          </Link>
          <Link to="/invoices" className={`nav-item ${isActive('/invoices') ? 'active' : ''}`}>
            Invoices
          </Link>
          <Link to="/parties" className={`nav-item ${isActive('/parties') ? 'active' : ''}`}>
            Buyers
          </Link>
          <Link to="/categories" className={`nav-item ${isActive('/categories') ? 'active' : ''}`}>
            Categories
          </Link>
          <Link to="/reports" className={`nav-item ${isActive('/reports') ? 'active' : ''}`}>
            Reports
          </Link>
          <Link to="/settings" className={`nav-item ${isActive('/settings') ? 'active' : ''}`}>
            Settings
          </Link>
        </nav>

        {/* Desktop actions: Clock, Create Order & Logout */}
        <Space size="large" className="desktop-actions-only">
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ClockCircleOutlined style={{ color: '#858076' }} />
            <Text type="secondary" style={{ fontSize: 13, fontWeight: 500 }}>
              {currentTime}
            </Text>
          </div>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => navigate('/new-bill')}
            style={{ height: '40px', padding: '0 20px' }}
          >
            Create Order
          </Button>

          {/* User profile & Sign Out */}
          {authenticated && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', borderLeft: '1px solid #e2e8f0', paddingLeft: '16px' }}>
              <Text strong style={{ color: '#1e293b', fontSize: 13 }}>
                {getUser()?.fullName.split(' ')[0]}
              </Text>
              <Button
                type="text"
                danger
                icon={<LogoutOutlined />}
                onClick={handleLogout}
                style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}
                title="Log Out"
              />
            </div>
          )}
        </Space>

        {/* Mobile Header actions: + Create Bill and logout buttons */}
        {authenticated && (
          <Space className="mobile-action-only" size="small">
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/new-bill')}
              style={{
                height: '36px',
                padding: '0 12px',
                fontSize: '13px',
                fontWeight: 600,
                background: 'var(--primary-color)',
                borderColor: 'var(--primary-color)'
              }}
            >
              + Invoice
            </Button>
            <Button
              type="default"
              shape="circle"
              danger
              icon={<LogoutOutlined />}
              onClick={handleLogout}
              style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '36px', width: '36px' }}
            />
          </Space>
        )}
      </header>

      {/* Main Content Area */}
      <Content style={{ minHeight: 'calc(100vh - 72px)', display: 'flex', flexDirection: 'column' }}>
        <div className="page-container" style={{ flex: 1 }}>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/invoices" element={<Invoices />} />
            <Route path="/invoices/:id" element={<InvoiceDetails />} />
            <Route path="/new-bill" element={<NewBill />} />
            <Route path="/edit-invoice/:id" element={<NewBill />} />
            <Route path="/parties" element={<Customers />} />
            <Route path="/products" element={<Products />} />
            <Route path="/categories" element={<Categories />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="*" element={<Dashboard />} />
          </Routes>
        </div>
        <Layout.Footer className="no-print" style={{ textAlign: 'center', color: '#858076', padding: '24px 24px 80px', background: '#faf9f6', borderTop: '1px solid #f1ebd9' }}>
          AQUA Sanitaryware Manufacturing &copy; {dayjs().format('YYYY')} — Secure GST B2B Ledger
        </Layout.Footer>
      </Content>

      {/* Bottom Sticky Navigation Bar for Mobile Viewports */}
      <div className="bottom-nav no-print">
        <Link to="/" className={`bottom-nav-item ${isActive('/') ? 'active' : ''}`}>
          <DashboardOutlined style={{ fontSize: '20px' }} />
          <span style={{ fontSize: '10px', marginTop: '2px' }}>Home</span>
        </Link>
        <Link to="/products" className={`bottom-nav-item ${isActive('/products') ? 'active' : ''}`}>
          <ShoppingOutlined style={{ fontSize: '20px' }} />
          <span style={{ fontSize: '10px', marginTop: '2px' }}>Catalog</span>
        </Link>
        <Link to="/invoices" className={`bottom-nav-item ${isActive('/invoices') ? 'active' : ''}`}>
          <FileTextOutlined style={{ fontSize: '20px' }} />
          <span style={{ fontSize: '10px', marginTop: '2px' }}>Invoices</span>
        </Link>
        <Link to="/parties" className={`bottom-nav-item ${isActive('/parties') ? 'active' : ''}`}>
          <TeamOutlined style={{ fontSize: '20px' }} />
          <span style={{ fontSize: '10px', marginTop: '2px' }}>Buyers</span>
        </Link>
        <Link to="/settings" className={`bottom-nav-item ${isActive('/settings') ? 'active' : ''}`}>
          <SettingOutlined style={{ fontSize: '20px' }} />
          <span style={{ fontSize: '10px', marginTop: '2px' }}>Settings</span>
        </Link>
      </div>

      {/* Side Slide-out Drawer Navigation for Mobile Devices */}
      <Drawer
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '20px', color: '#855b14', display: 'flex', alignItems: 'center' }}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" />
              </svg>
            </span>
            <span style={{ fontSize: '18px', fontWeight: 700, letterSpacing: '2px', color: '#1e293b' }}>AQUA</span>
          </div>
        }
        placement="left"
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
        width={260}
        bodyStyle={{ padding: '8px 0', display: 'flex', flexDirection: 'column' }}
      >
        <div style={{ flex: 1 }}>
          <Link to="/" className={`drawer-menu-item ${isActive('/') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <DashboardOutlined /> <span>Dashboard</span>
          </Link>
          <Link to="/products" className={`drawer-menu-item ${isActive('/products') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <ShoppingOutlined /> <span>Catalog</span>
          </Link>
          <Link to="/invoices" className={`drawer-menu-item ${isActive('/invoices') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <FileTextOutlined /> <span>Invoices</span>
          </Link>
          <Link to="/parties" className={`drawer-menu-item ${isActive('/parties') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <TeamOutlined /> <span>Buyers</span>
          </Link>
          <Link to="/categories" className={`drawer-menu-item ${isActive('/categories') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <AppstoreOutlined /> <span>Categories</span>
          </Link>
          <Link to="/reports" className={`drawer-menu-item ${isActive('/reports') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <BarChartOutlined /> <span>Reports</span>
          </Link>
          <Link to="/settings" className={`drawer-menu-item ${isActive('/settings') ? 'active' : ''}`} onClick={() => setDrawerVisible(false)}>
            <SettingOutlined /> <span>Settings</span>
          </Link>
        </div>
        <div style={{ padding: '16px', borderTop: '1px solid #e2e8f0' }}>
          <Button
            type="primary"
            danger
            block
            icon={<LogoutOutlined />}
            onClick={() => {
              setDrawerVisible(false);
              handleLogout();
            }}
          >
            Log Out
          </Button>
        </div>
      </Drawer>
    </Layout>
  );
};

export default App;
