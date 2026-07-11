import React, { useState, useEffect } from 'react';
import { Table, Button, Space, Input, Select, Tag, Alert, Typography, message, Popconfirm, Card } from 'antd';
import { SearchOutlined, EyeOutlined, CheckCircleOutlined, CloseCircleOutlined, DownloadOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Paragraph } = Typography;
const { Option } = Select;

interface InvoiceListItem {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  partyName: string;
  subTotal: number;
  totalGst: number;
  grandTotal: number;
  status: string;
}

const Invoices: React.FC = () => {
  const [invoices, setInvoices] = useState<InvoiceListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Filtering & Pagination state
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotal] = useState(0);

  const fetchInvoices = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const params: any = {
        page,
        pageSize,
      };
      if (search) params.search = search;
      if (status) params.status = status;

      const res = await api.get('/invoices', { params });
      if (res.success) {
        setInvoices(res.data.items);
        setTotal(res.data.totalCount);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load invoices catalog');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchInvoices();
  }, [page, pageSize, status]);

  const handleSearch = () => {
    setPage(1);
    fetchInvoices();
  };

  const handleCancelInvoice = async (id: string) => {
    try {
      const res = await api.delete(`/invoices/${id}`);
      if (res.success) {
        message.success('Invoice cancelled successfully. Inventory stock levels restored.');
        fetchInvoices();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to cancel invoice');
    }
  };

  const handleDownloadPdf = async (id: string, invoiceNumber: string) => {
    try {
      message.loading({ content: 'Generating PDF...', key: 'pdf_download' });
      // Request PDF as Blob
      const response = await api.get(`/invoices/${id}/pdf`, { responseType: 'blob' });
      
      // Axios response interceptor intercepts raw returns.
      // But since responseType is 'blob', axios might wrap it. Let's make sure it handles it:
      const blob = response instanceof Blob ? response : new Blob([response as any]);
      
      const link = document.createElement('a');
      link.href = window.URL.createObjectURL(blob);
      link.download = `${invoiceNumber}.pdf`;
      link.click();
      message.success({ content: 'PDF downloaded successfully', key: 'pdf_download' });
    } catch (err: any) {
      message.error({ content: 'Failed to generate PDF: ' + err.message, key: 'pdf_download' });
    }
  };

  const columns = [
    {
      title: 'Invoice No.',
      dataIndex: 'invoiceNumber',
      key: 'invoiceNumber',
      render: (text: string, r: InvoiceListItem) => <Link to={`/invoices/${r.id}`} style={{ fontWeight: 600 }}>{text}</Link>,
    },
    {
      title: 'Customer (Party)',
      dataIndex: 'partyName',
      key: 'partyName',
      render: (text: string) => <Typography.Text strong>{text}</Typography.Text>,
    },
    {
      title: 'Date',
      dataIndex: 'invoiceDate',
      key: 'invoiceDate',
      render: (date: string) => dayjs(date).format('DD MMM YYYY'),
    },
    {
      title: 'Net Amount',
      dataIndex: 'subTotal',
      key: 'subTotal',
      render: (val: number) => `₹${val.toFixed(2)}`,
    },
    {
      title: 'GST (5%)',
      dataIndex: 'totalGst',
      key: 'totalGst',
      render: (val: number) => `₹${val.toFixed(2)}`,
    },
    {
      title: 'Grand Total',
      dataIndex: 'grandTotal',
      key: 'grandTotal',
      render: (val: number) => <Typography.Text strong>₹{val.toFixed(2)}</Typography.Text>,
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
    {
      title: 'Actions',
      key: 'actions',
      render: (r: InvoiceListItem) => (
        <Space size="middle">
          <Button icon={<EyeOutlined />} type="link">
            <Link to={`/invoices/${r.id}`}>View</Link>
          </Button>
          <Button icon={<DownloadOutlined />} type="link" onClick={() => handleDownloadPdf(r.id, r.invoiceNumber)}>
            PDF
          </Button>
          {r.status !== 'cancelled' && (
            <Popconfirm
              title="Cancel Invoice?"
              description="This will mark invoice as cancelled and return items to inventory."
              onConfirm={() => handleCancelInvoice(r.id)}
              okText="Yes"
              cancelText="No"
            >
              <Button type="link" danger icon={<CloseCircleOutlined />}>
                Cancel
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'between', alignItems: 'center', marginBottom: 20 }}>
        <div style={{ flex: 1 }}>
          <Title level={3} style={{ margin: 0 }}>Invoices History</Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            Audit trail of generated white bills. Review billing details, track payments, download GST receipts.
          </Paragraph>
        </div>
      </div>

      {error && <Alert message="Error" description={error} type="error" showIcon style={{ marginBottom: 16 }} />}

      {/* Filters Area */}
      <Card style={{ marginBottom: 16, borderRadius: 8 }} size="small">
        <Space size="middle" wrap>
          <Input
            placeholder="Search party or invoice..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onPressEnter={handleSearch}
            style={{ width: 220 }}
          />
          <Select
            placeholder="Filter Status"
            allowClear
            value={status}
            onChange={(val) => setStatus(val)}
            style={{ width: 150 }}
          >
            <Option value="pending">Pending</Option>
            <Option value="paid">Paid</Option>
            <Option value="cancelled">Cancelled</Option>
          </Select>
          <Button type="primary" onClick={handleSearch}>
            Apply Filters
          </Button>
        </Space>
      </Card>

      <Table
        dataSource={invoices}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={{
          current: page,
          pageSize: pageSize,
          total: total,
          onChange: (p, ps) => {
            setPage(p);
            setPageSize(ps);
          },
        }}
        size="middle"
      />
    </div>
  );
};

export default Invoices;
