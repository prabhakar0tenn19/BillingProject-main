import React, { useState, useEffect } from 'react';
import { Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import { Layout, Button, Space, Typography } from 'antd';
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
  PlusOutlined
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

const { Content } = Layout;
const { Text } = Typography;

const App: React.FC = () => {
  const [currentTime, setCurrentTime] = useState(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(dayjs().format('DD MMM YYYY, hh:mm:ss A'));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const isActive = (path: string) => {
    if (path === '/' && location.pathname === '/') return true;
    if (path !== '/' && location.pathname.startsWith(path)) return true;
    return false;
  };

  return (
    <Layout style={{ minHeight: '100vh', background: '#faf9f6' }}>
      {/* AQUA Top Navigation Header */}
      <header className="header-nav no-print">
        <div className="logo-container" onClick={() => navigate('/')}>
          {/* Gold building/water drop icon */}
          <span style={{ fontSize: '24px', color: '#855b14', display: 'flex', alignItems: 'center' }}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" />
            </svg>
          </span>
          <span className="logo-text">AQUA</span>
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

        {/* Desktop actions: Clock & Create Order */}
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
        </Space>

        {/* Mobile Header action: Simple circular plus button */}
        <Button
          type="primary"
          shape="circle"
          icon={<PlusOutlined />}
          onClick={() => navigate('/new-bill')}
          className="mobile-action-only"
        />
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
    </Layout>
  );
};

export default App;
