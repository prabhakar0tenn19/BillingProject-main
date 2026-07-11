import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { Card, Button, Space, Tag, Alert, Spin, Typography, Divider, Table, Modal, Form, Select, DatePicker, message } from 'antd';
import {
  ArrowLeftOutlined, DownloadOutlined, PrinterOutlined,
  CheckCircleOutlined, InfoCircleOutlined, WalletOutlined, RollbackOutlined, EditOutlined
} from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

interface InvoiceDetail {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string;
  status: string;
  paidAt?: string;
  paymentMode?: string;
  subTotal: number;
  totalGst: number;
  grandTotal: number;
  totalInWords: string;
  remarks?: string;
  customerSnapshot: {
    partyName: string;
    contactPerson?: string;
    phone?: string;
    billingAddress?: string;
    gstin?: string;
  };
  companySnapshot: {
    companyName: string;
    address: string;
    gstin: string;
    phone: string;
    email?: string;
    signatureUrl?: string;
    bankName?: string;
    bankAccount?: string;
    bankIfsc?: string;
  };
  items: Array<{
    productId: string;
    productName: string;
    modelNumber: string;
    hsnCode: string;
    quantity: number;
    rate: number;
    subTotal: number;
    gstAmount: number;
    lineTotal: number;
  }>;
}

const InvoiceDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [invoice, setInvoice] = useState<InvoiceDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Payment Modal state
  const [isPayModalOpen, setIsPayModalOpen] = useState(false);
  const [payForm] = Form.useForm();
  const [paymentSubmitting, setPaymentSubmitting] = useState(false);

  const fetchInvoiceDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await api.get(`/invoices/${id}`);
      if (res.success) {
        setInvoice(res.data);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load invoice details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (id) fetchInvoiceDetails();
  }, [id]);

  const handleDownloadPdf = async () => {
    if (!invoice) return;
    try {
      message.loading({ content: 'Generating PDF...', key: 'pdf_download' });
      const response = await api.get(`/invoices/${invoice.id}/pdf`, { responseType: 'blob' });
      const blob = response instanceof Blob ? response : new Blob([response as any]);
      const link = document.createElement('a');
      link.href = window.URL.createObjectURL(blob);
      link.download = `${invoice.invoiceNumber}.pdf`;
      link.click();
      message.success({ content: 'PDF downloaded successfully', key: 'pdf_download' });
    } catch (err: any) {
      message.error({ content: 'Failed to generate PDF: ' + err.message, key: 'pdf_download' });
    }
  };

  const handlePrint = () => {
    window.print();
  };

  const handleRecordPayment = async () => {
    if (!invoice) return;
    try {
      setPaymentSubmitting(true);
      const values = await payForm.validateFields();
      
      const payload = {
        paymentMode: values.paymentMode,
        paidAt: values.paidAt ? values.paidAt.toISOString() : new Date().toISOString()
      };

      const res = await api.patch(`/invoices/${invoice.id}/mark-paid`, payload);
      if (res.success) {
        message.success('Payment recorded successfully');
        setIsPayModalOpen(false);
        fetchInvoiceDetails();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to record payment');
    } finally {
      setPaymentSubmitting(false);
    }
  };

  const handleMarkAsPending = async () => {
    if (!invoice) return;
    try {
      const res = await api.patch(`/invoices/${invoice.id}/mark-pending`, {});
      if (res.success) {
        message.success('Invoice marked as pending');
        fetchInvoiceDetails();
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to update invoice status');
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '350px' }}>
        <Spin size="large" tip="Fetching invoice details..." />
      </div>
    );
  }

  if (error || !invoice) {
    return (
      <div style={{ padding: 24 }}>
        <Alert message="Error" description={error || 'Invoice not found'} type="error" showIcon />
        <Button icon={<ArrowLeftOutlined />} style={{ marginTop: 16 }}>
          <Link to="/invoices">Back to Invoices</Link>
        </Button>
      </div>
    );
  }

  // Table Columns
  const columns = [
    {
      title: 'Sr.',
      key: 'serial',
      width: 50,
      render: (_: any, __: any, index: number) => index + 1,
    },
    {
      title: 'Description & Model',
      key: 'product',
      render: (r: any) => (
        <div>
          <Text strong>{r.productName}</Text>
          <div style={{ fontSize: '11px', color: '#64748b' }}>Model: {r.modelNumber}</div>
        </div>
      ),
    },
    {
      title: 'HSN',
      dataIndex: 'hsnCode',
      key: 'hsnCode',
    },
    {
      title: 'Qty',
      dataIndex: 'quantity',
      key: 'quantity',
      align: 'right' as const,
    },
    {
      title: 'Rate (₹)',
      dataIndex: 'rate',
      key: 'rate',
      align: 'right' as const,
      render: (rate: number) => rate.toFixed(2),
    },
    {
      title: 'Taxable (₹)',
      dataIndex: 'subTotal',
      key: 'subTotal',
      align: 'right' as const,
      render: (sub: number) => sub.toFixed(2),
    },
    {
      title: 'CGST 2.5% (₹)',
      key: 'cgst',
      align: 'right' as const,
      render: (r: any) => (r.gstAmount / 2).toFixed(2),
    },
    {
      title: 'SGST 2.5% (₹)',
      key: 'sgst',
      align: 'right' as const,
      render: (r: any) => (r.gstAmount / 2).toFixed(2),
    },
    {
      title: 'Total (₹)',
      dataIndex: 'lineTotal',
      key: 'lineTotal',
      align: 'right' as const,
      render: (val: number) => <Text strong>{val.toFixed(2)}</Text>,
    },
  ];

  return (
    <div>
      {/* Action Header Panel */}
      <div className="no-print" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, flexWrap: 'wrap', gap: 16 }}>
        <Button icon={<ArrowLeftOutlined />} size="large">
          <Link to="/invoices">Back to Invoices</Link>
        </Button>
        <Space size="middle" wrap>
          {invoice.status === 'pending' && (
            <>
              <Button type="primary" icon={<WalletOutlined />} onClick={() => setIsPayModalOpen(true)} size="large">
                Record Payment
              </Button>
              <Button icon={<EditOutlined />} size="large">
                <Link to={`/edit-invoice/${invoice.id}`}>Edit Invoice</Link>
              </Button>
            </>
          )}
          {invoice.status === 'paid' && (
            <Button icon={<RollbackOutlined />} danger onClick={handleMarkAsPending} size="large">
              Mark Unpaid / Pending
            </Button>
          )}
          <Button icon={<PrinterOutlined />} onClick={handlePrint} size="large">
            Print Bill
          </Button>
          <Button type="primary" icon={<DownloadOutlined />} onClick={handleDownloadPdf} size="large">
            Download PDF
          </Button>
        </Space>
      </div>

      {/* Invoice Card Container */}
      <Card className="print-invoice-container" style={{ borderRadius: 10, border: '1px solid #e2e8f0', boxShadow: '0 4px 12px rgba(0,0,0,0.02)', padding: '16px' }}>
        
        {/* Invoice Header Details */}
        <div className="invoice-header-row">
          <div>
            <Title level={3} style={{ margin: 0, color: '#1e293b' }}>TAX INVOICE</Title>
            <Text type="secondary">GST-compliant Manufacturer Invoice</Text>
          </div>
          <div style={{ textAlign: 'right' }}>
            <Title level={4} style={{ margin: 0 }}>{invoice.invoiceNumber}</Title>
            <Space>
              <Text type="secondary">Date: {dayjs(invoice.invoiceDate).format('DD MMM YYYY')}</Text>
              <Tag color={invoice.status === 'paid' ? 'green' : invoice.status === 'cancelled' ? 'red' : 'gold'}>
                {invoice.status.toUpperCase()}
              </Tag>
            </Space>
          </div>
        </div>

        {/* Addresses Box */}
        <div className="invoice-addresses-grid">
          {/* Supplier details (Company snapshot) */}
          <div style={{ background: '#f8fafc', padding: 16, borderRadius: 8 }}>
            <Text type="secondary" strong style={{ display: 'block', marginBottom: 8 }}>FROM (SUPPLIER)</Text>
            <Title level={5} style={{ margin: '0 0 4px' }}>{invoice.companySnapshot.companyName}</Title>
            <Paragraph style={{ margin: '0 0 8px', fontSize: 13 }}>
              {invoice.companySnapshot.address}
            </Paragraph>
            <div style={{ fontSize: 13 }}>
              <Text strong>GSTIN: </Text> <Text>{invoice.companySnapshot.gstin}</Text>
              <br />
              <Text strong>Phone: </Text> <Text>{invoice.companySnapshot.phone}</Text>
              {invoice.companySnapshot.email && (
                <>
                  <br />
                  <Text strong>Email: </Text> <Text>{invoice.companySnapshot.email}</Text>
                </>
              )}
            </div>
          </div>

          {/* Customer details (Customer snapshot) */}
          <div style={{ background: '#f8fafc', padding: 16, borderRadius: 8 }}>
            <Text type="secondary" strong style={{ display: 'block', marginBottom: 8 }}>TO (RECIPIENT / PARTY)</Text>
            <Title level={5} style={{ margin: '0 0 4px' }}>{invoice.customerSnapshot.partyName}</Title>
            <Paragraph style={{ margin: '0 0 8px', fontSize: 13 }}>
              {invoice.customerSnapshot.billingAddress || 'No Address Listed'}
            </Paragraph>
            <div style={{ fontSize: 13 }}>
              <Text strong>GSTIN: </Text> <Text>{invoice.customerSnapshot.gstin || 'Unregistered'}</Text>
              <br />
              <Text strong>Contact: </Text> <Text>{invoice.customerSnapshot.contactPerson || 'N/A'}</Text>
              <br />
              <Text strong>Phone: </Text> <Text>{invoice.customerSnapshot.phone || 'N/A'}</Text>
            </div>
          </div>
        </div>

        {/* Invoice Items Table */}
        <Table
          dataSource={invoice.items}
          columns={columns}
          rowKey="productId"
          pagination={false}
          scroll={{ x: 'max-content' }}
          size="middle"
          bordered
          style={{ marginBottom: 32 }}
        />

        {/* Totals, Bank details and Digital Signature */}
        <div className="invoice-totals-row">
          {/* Left panel: Bank Details and Total in Words */}
          <div>
            {invoice.companySnapshot.bankName && (
              <div style={{ background: '#f8fafc', padding: 12, borderRadius: 8, marginBottom: 16, fontSize: 12 }}>
                <Text strong style={{ display: 'block', marginBottom: 4 }}>BANK PAYMENT DETAILS</Text>
                <div className="invoice-bank-grid">
                  <Text type="secondary">Bank Name: </Text> <Text strong>{invoice.companySnapshot.bankName}</Text>
                  <Text type="secondary">Account No: </Text> <Text strong>{invoice.companySnapshot.bankAccount}</Text>
                  <Text type="secondary">IFSC Code: </Text> <Text strong>{invoice.companySnapshot.bankIfsc}</Text>
                </div>
              </div>
            )}
            <div>
              <Text type="secondary">Amount in Words: </Text>
              <br />
              <Text strong style={{ fontSize: 13, textTransform: 'capitalize' }}>
                Rupees {invoice.totalInWords} Only.
              </Text>
            </div>
            {invoice.remarks && (
              <div style={{ marginTop: 16, fontSize: 12 }}>
                <Text type="secondary" strong>Notes/Remarks: </Text>
                <Text>{invoice.remarks}</Text>
              </div>
            )}
          </div>

          {/* Right panel: Calculations summary & signature */}
          <div style={{ textAlign: 'right' }}>
            <div className="invoice-calc-grid">
              <Text type="secondary">Taxable SubTotal: </Text>
              <Text strong>₹{invoice.subTotal.toFixed(2)}</Text>
              
              <Text type="secondary">Total CGST (2.5%): </Text>
              <Text strong>₹{(invoice.totalGst / 2).toFixed(2)}</Text>
              
              <Text type="secondary">Total SGST (2.5%): </Text>
              <Text strong>₹{(invoice.totalGst / 2).toFixed(2)}</Text>

              <Text type="secondary">Total GST (5%): </Text>
              <Text strong>₹{invoice.totalGst.toFixed(2)}</Text>
              
              <Text strong style={{ fontSize: 15 }}>Grand Total: </Text>
              <Text strong style={{ fontSize: 17, color: '#1677ff' }}>₹{Math.round(invoice.grandTotal).toFixed(2)}</Text>
            </div>

            {/* Official Stamp / Signature Section */}
            <div style={{ marginTop: 32, display: 'inline-block', textAlign: 'center' }}>
              <Text type="secondary" style={{ display: 'block', fontSize: 11, marginBottom: 8 }}>
                For {invoice.companySnapshot.companyName}
              </Text>
              {invoice.companySnapshot.signatureUrl ? (
                <div>
                  <img
                    src={invoice.companySnapshot.signatureUrl}
                    alt="Authorized Stamp/Signature"
                    style={{ maxHeight: 70, maxWidth: 160, objectFit: 'contain' }}
                  />
                  <Divider style={{ margin: '4px 0' }} />
                  <Text strong style={{ fontSize: 10, display: 'block' }}>Authorized Signatory</Text>
                </div>
              ) : (
                <div style={{ height: 60, width: 150, border: '1px dashed #cbd5e1', display: 'flex', alignItems: 'center', justifyContent: 'center', borderRadius: 4 }}>
                  <Text type="secondary" style={{ fontSize: 10 }}>Stamp/Signature Missing</Text>
                </div>
              )}
            </div>
          </div>
        </div>

      </Card>

      {/* Record Payment Modal */}
      <Modal
        title="Record Invoice Payment"
        open={isPayModalOpen}
        onOk={handleRecordPayment}
        onCancel={() => setIsPayModalOpen(false)}
        confirmLoading={paymentSubmitting}
        okText="Record Payment"
        destroyOnClose
      >
        <Form form={payForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="paymentMode"
            label="Payment Mode"
            initialValue="upi"
            rules={[{ required: true, message: 'Please select a payment mode' }]}
          >
            <Select>
              <Option value="upi">UPI / QR Code</Option>
              <Option value="neft">NEFT / Bank Transfer</Option>
              <Option value="cheque">Cheque</Option>
              <Option value="cash">Cash</Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="paidAt"
            label="Payment Date"
            initialValue={dayjs()}
            rules={[{ required: true, message: 'Please select payment date' }]}
          >
            <DatePicker style={{ width: '100%' }} format="DD MMM YYYY" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default InvoiceDetails;
