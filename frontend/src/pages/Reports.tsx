import React, { useState, useEffect } from 'react';
import { Card, Table, Space, DatePicker, Button, Typography, Alert, Spin, Tag, Statistic, Row, Col } from 'antd';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { SearchOutlined, DownloadOutlined, FileTextOutlined } from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Paragraph, Text } = Typography;
const { RangePicker } = DatePicker;

interface CustomerSales {
  customerId: string;
  partyName: string;
  totalSales: number;
  invoiceCount: number;
}

interface ProductSales {
  productId: string;
  productName: string;
  modelNumber: string;
  totalRevenue: number;
  totalQuantitySold: number;
}

interface GstReport {
  taxableAmount: number;
  totalGst: number;
  cgstCollected: number;
  sgstCollected: number;
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#AF19FF', '#FF19A3'];

const Reports: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Date range state (default to last 30 days)
  const [dates, setDates] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, 'day'),
    dayjs()
  ]);

  // Data states
  const [customerSales, setCustomerSales] = useState<CustomerSales[]>([]);
  const [productSales, setProductSales] = useState<ProductSales[]>([]);
  const [gstSummary, setGstSummary] = useState<GstReport | null>(null);

  const fetchReportData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Backend expects 'from' and 'to' query parameters
      const params = {
        from: dates[0].startOf('day').toISOString(),
        to: dates[1].endOf('day').toISOString()
      };

      const [custRes, prodRes, gstRes] = await Promise.all([
        api.get('/reports/customer-sales', { params }),
        api.get('/reports/product-sales', { params }),
        api.get('/reports/gst-summary', { params }) // Corrected endpoint from /reports/gst to /reports/gst-summary
      ]);

      if (custRes.success) setCustomerSales(custRes.data);
      if (prodRes.success) setProductSales(prodRes.data);
      if (gstRes.success) setGstSummary(gstRes.data);
    } catch (err: any) {
      setError(err.message || 'Failed to generate reports');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReportData();
  }, []);

  const handleApplyRange = () => {
    fetchReportData();
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20, flexWrap: 'wrap', gap: 16 }}>
        <div style={{ flex: 1 }}>
          <Title level={3} style={{ margin: 0 }}>Business Analytics & Tax Reports</Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            Generate white ledger reports. Extract GSTR summaries, client business volume, and top inventory items.
          </Paragraph>
        </div>
        <Space>
          <RangePicker
            value={dates}
            onChange={(val) => {
              if (val && val[0] && val[1]) setDates([val[0], val[1]]);
            }}
            format="DD MMM YYYY"
            size="large"
          />
          <Button type="primary" icon={<SearchOutlined />} onClick={handleApplyRange} size="large">
            Generate Report
          </Button>
        </Space>
      </div>

      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 24 }} />}

      {loading ? (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '300px' }}>
          <Spin size="large" tip="Generating charts & spreadsheets..." />
        </div>
      ) : (
        <div>
          {/* Tax summary metrics */}
          {gstSummary && (
            <Card style={{ marginBottom: 24, borderRadius: 10, border: '1px solid #e2e8f0' }}>
              <Row gutter={16}>
                <Col span={6}>
                  <Statistic
                    title="Total Taxable Revenue (Net)"
                    value={gstSummary.taxableAmount}
                    precision={2}
                    prefix="₹"
                    valueStyle={{ color: '#0f172a' }}
                  />
                </Col>
                <Col span={6}>
                  <Statistic
                    title="Total GST Collected (5%)"
                    value={gstSummary.totalGst}
                    precision={2}
                    prefix="₹"
                    valueStyle={{ color: '#10b981' }}
                  />
                </Col>
                <Col span={6}>
                  <Statistic
                    title="CGST Share (2.5%)"
                    value={gstSummary.cgstCollected}
                    precision={2}
                    prefix="₹"
                    valueStyle={{ color: '#3b82f6' }}
                  />
                </Col>
                <Col span={6}>
                  <Statistic
                    title="SGST Share (2.5%)"
                    value={gstSummary.sgstCollected}
                    precision={2}
                    prefix="₹"
                    valueStyle={{ color: '#3b82f6' }}
                  />
                </Col>
              </Row>
            </Card>
          )}

          <Row gutter={24} style={{ marginBottom: 24 }}>
            {/* Top Parties Chart */}
            <Col xs={24} lg={12}>
              <Card title="Sales Breakdown by Party" style={{ borderRadius: 10 }}>
                <div style={{ width: '100%', height: 260 }}>
                  <ResponsiveContainer>
                    <BarChart data={customerSales.slice(0, 5)}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} />
                      <XAxis dataKey="partyName" tickLine={false} style={{ fontSize: 11 }} />
                      <YAxis tickLine={false} style={{ fontSize: 11 }} />
                      <Tooltip formatter={(val) => `₹${Number(val).toFixed(2)}`} />
                      <Legend />
                      <Bar name="Sales Volume" dataKey="totalSales" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </Card>
            </Col>

            {/* Top Products Pie Chart */}
            <Col xs={24} lg={12}>
              <Card title="Top Selling Products" style={{ borderRadius: 10 }}>
                <div style={{ width: '100%', height: 260, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  {productSales.length > 0 ? (
                    <ResponsiveContainer>
                      <PieChart>
                        <Pie
                          data={productSales.slice(0, 5)}
                          cx="50%"
                          cy="50%"
                          labelLine={false}
                          label={({ name, percent }) => `${name} (${((percent || 0) * 100).toFixed(0)}%)`}
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="totalRevenue"
                          nameKey="productName"
                        >
                          {productSales.slice(0, 5).map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                          ))}
                        </Pie>
                        <Tooltip formatter={(val) => `₹${Number(val).toFixed(2)}`} />
                      </PieChart>
                    </ResponsiveContainer>
                  ) : (
                    <Text type="secondary">No sales recorded in this range</Text>
                  )}
                </div>
              </Card>
            </Col>
          </Row>

          <Row gutter={24}>
            {/* Customer List table */}
            <Col xs={24} lg={12}>
              <Card title="Parties Sales Volume Ledger" style={{ borderRadius: 10 }}>
                <Table
                  dataSource={customerSales}
                  rowKey="customerId"
                  pagination={{ pageSize: 5 }}
                  size="small"
                  columns={[
                    { title: 'Party Name', dataIndex: 'partyName', key: 'name' },
                    { title: 'Invoices', dataIndex: 'invoiceCount', key: 'count', align: 'center' },
                    { title: 'Total Volume', dataIndex: 'totalSales', key: 'sales', align: 'right', render: (val) => `₹${val.toFixed(2)}` },
                  ]}
                />
              </Card>
            </Col>

            {/* Product List table */}
            <Col xs={24} lg={12}>
              <Card title="Products Quantity Sales Ledger" style={{ borderRadius: 10 }}>
                <Table
                  dataSource={productSales}
                  rowKey="productId"
                  pagination={{ pageSize: 5 }}
                  size="small"
                  columns={[
                    { title: 'Product Model', key: 'name', render: (r) => `${r.productName} (${r.modelNumber})` },
                    { title: 'Qty Sold', dataIndex: 'totalQuantitySold', key: 'qty', align: 'center' },
                    { title: 'Total Revenue', dataIndex: 'totalRevenue', key: 'sales', align: 'right', render: (val) => `₹${val.toFixed(2)}` },
                  ]}
                />
              </Card>
            </Col>
          </Row>
        </div>
      )}
    </div>
  );
};

export default Reports;
