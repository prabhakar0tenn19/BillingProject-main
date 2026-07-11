import React, { useState, useEffect } from 'react';
import { Card, Table, Alert, Spin, Typography, Space, Tag } from 'antd';
import {
  DollarCircleOutlined,
  ClockCircleOutlined,
  TeamOutlined,
  WarningOutlined,
  ArrowUpOutlined,
} from '@ant-design/icons';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import api from '../api';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';

const { Title, Text } = Typography;

interface DashboardSummary {
  totalSales: number;
  pendingAmount: number;
  activeCustomers: number;
  lowStockCount: number;
}

interface MonthlySale {
  month: string;
  sales: number;
}

interface InvoiceItem {
  id: string;
  invoiceNumber: string;
  partyName: string;
  invoiceDate: string;
  grandTotal: number;
  status: string;
}

const Dashboard: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [chartData, setChartData] = useState<MonthlySale[]>([]);
  const [recentInvoices, setRecentInvoices] = useState<InvoiceItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        // Call concurrent endpoints
        const [summaryRes, chartRes, invoicesRes] = await Promise.all([
          api.get('/dashboard/summary'),
          api.get('/dashboard/monthly-sales'),
          api.get('/dashboard/recent-invoices'),
        ]);

        if (summaryRes.success) setSummary(summaryRes.data);
        if (chartRes.success) setChartData(chartRes.data);
        if (invoicesRes.success) setRecentInvoices(invoicesRes.data);
      } catch (err: any) {
        setError(err.message || 'Failed to load dashboard data');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '300px' }}>
        <Spin size="large" tip="Loading Dashboard Data..." />
      </div>
    );
  }

  if (error) {
    return <Alert message="Error" description={error} type="error" showIcon style={{ margin: '16px 0' }} />;
  }

  const columns = [
    {
      title: 'Invoice No.',
      dataIndex: 'invoiceNumber',
      key: 'invoiceNumber',
      render: (text: string, record: InvoiceItem) => <Link to={`/invoices/${record.id}`}>{text}</Link>,
    },
    {
      title: 'Customer (Party)',
      dataIndex: 'partyName',
      key: 'partyName',
    },
    {
      title: 'Date',
      dataIndex: 'invoiceDate',
      key: 'invoiceDate',
      render: (date: string) => dayjs(date).format('DD MMM YYYY'),
    },
    {
      title: 'Total Amount',
      dataIndex: 'grandTotal',
      key: 'grandTotal',
      render: (amount: number) => <Text strong>₹{amount.toFixed(2)}</Text>,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        let color = 'gold';
        if (status === 'paid') color = 'green';
        if (status === 'cancelled') color = 'red';
        return <Tag color={color}>{status.toUpperCase()}</Tag>;
      },
    },
  ];

  return (
    <div>
      {/* Metrics Row */}
      <div className="metrics-grid">
        <div className="metric-card">
          <div className="metric-info">
            <h3>TOTAL SALES (WHITE)</h3>
            <h2>₹{(summary?.totalSales || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</h2>
          </div>
          <div className="metric-icon-wrapper" style={{ background: '#ecfdf5', color: '#10b981' }}>
            <DollarCircleOutlined />
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-info">
            <h3>PENDING PAYMENTS</h3>
            <h2>₹{(summary?.pendingAmount || 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</h2>
          </div>
          <div className="metric-icon-wrapper" style={{ background: '#fffbeb', color: '#f59e0b' }}>
            <ClockCircleOutlined />
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-info">
            <h3>ACTIVE PARTIES</h3>
            <h2>{summary?.activeCustomers || 0}</h2>
          </div>
          <div className="metric-icon-wrapper" style={{ background: '#eff6ff', color: '#3b82f6' }}>
            <TeamOutlined />
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-info">
            <h3>LOW STOCK PRODUCTS</h3>
            <h2>{summary?.lowStockCount || 0}</h2>
          </div>
          <div className="metric-icon-wrapper" style={{ background: '#fef2f2', color: '#ef4444' }}>
            <WarningOutlined />
          </div>
        </div>
      </div>

      {/* Chart Section */}
      <Card title="Sales Growth Chart (GST Billing)" style={{ marginBottom: 24, borderRadius: 10 }}>
        <div style={{ width: '100%', height: 300 }}>
          <ResponsiveContainer>
            <AreaChart data={chartData} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="colorSales" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
              <XAxis dataKey="month" tickLine={false} axisLine={false} style={{ fontSize: 12, fill: '#64748b' }} />
              <YAxis tickLine={false} axisLine={false} style={{ fontSize: 12, fill: '#64748b' }} />
              <Tooltip formatter={(value) => [`₹${value}`, 'Sales']} />
              <Area type="monotone" dataKey="sales" stroke="#3b82f6" strokeWidth={2} fillOpacity={1} fill="url(#colorSales)" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </Card>

      {/* Recent Invoices Table */}
      <Card title="Recent Invoices" style={{ borderRadius: 10 }}>
        <Table
          dataSource={recentInvoices}
          columns={columns}
          rowKey="id"
          pagination={false}
          size="middle"
        />
      </Card>
    </div>
  );
};

export default Dashboard;
