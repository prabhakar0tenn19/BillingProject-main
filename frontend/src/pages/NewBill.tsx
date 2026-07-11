import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Steps, Card, Button, Form, Select, Table, InputNumber, Space,
  Divider, DatePicker, Input, Alert, message, Typography
} from 'antd';
import {
  UserOutlined, ShoppingCartOutlined, FileDoneOutlined,
  PlusOutlined, DeleteOutlined, CreditCardOutlined
} from '@ant-design/icons';
import api from '../api';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Option } = Select;

interface CustomerLookup {
  id: string;
  partyName: string;
  gstin?: string;
}

interface BillReadyProduct {
  productId: string;
  name: string;
  modelNumber: string;
  hsnCode: string;
  effectivePrice: number;
  hasCustomPrice: boolean;
  stock: number;
}

interface SelectedItem {
  key: string; // productId
  productId: string;
  name: string;
  modelNumber: string;
  hsnCode: string;
  quantity: number;
  rate: number;
  subTotal: number;
  gstRate: number;
  gstAmount: number;
  lineTotal: number;
}

const NewBill: React.FC = () => {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(0);
  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [customerCatalog, setCustomerCatalog] = useState<BillReadyProduct[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null);
  
  // Line items state
  const [selectedItems, setSelectedItems] = useState<SelectedItem[]>([]);
  
  // Loaders
  const [loadingCustomers, setLoadingCustomers] = useState(false);
  const [loadingCatalog, setLoadingCatalog] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Forms
  const [partyForm] = Form.useForm();
  const [metaForm] = Form.useForm();

  useEffect(() => {
    const fetchCustomers = async () => {
      try {
        setLoadingCustomers(true);
        const res = await api.get('/customers');
        if (res.success) {
          setCustomers(res.data.map((c: any) => ({
            id: c.id,
            partyName: c.partyName,
            gstin: c.gstin
          })));
        }
      } catch (err: any) {
        message.error('Failed to load customers: ' + err.message);
      } finally {
        setLoadingCustomers(false);
      }
    };
    fetchCustomers();
  }, []);

  const handleCustomerChange = async (customerId: string) => {
    setSelectedCustomerId(customerId);
    setSelectedItems([]); // reset items if customer changes
    try {
      setLoadingCatalog(true);
      const res = await api.get(`/customers/${customerId}/pricing/bill-ready`);
      if (res.success) {
        setCustomerCatalog(res.data);
      }
    } catch (err: any) {
      message.error('Failed to load pricing catalog: ' + err.message);
    } finally {
      setLoadingCatalog(false);
    }
  };

  const handleAddItem = (productId: string) => {
    const product = customerCatalog.find(p => p.productId === productId);
    if (!product) return;

    // Check if item already added
    if (selectedItems.some(item => item.productId === productId)) {
      message.warning('Product already added to line items. Adjust quantity instead.');
      return;
    }

    const rate = product.effectivePrice;
    const qty = 1;
    const sub = rate * qty;
    const gstRate = 5.0; // Fixed 5% GST for Sanitaryware business
    const gstAmt = Math.round(sub * gstRate) / 100;
    const total = sub + gstAmt;

    const newItem: SelectedItem = {
      key: product.productId,
      productId: product.productId,
      name: product.name,
      modelNumber: product.modelNumber,
      hsnCode: product.hsnCode,
      quantity: qty,
      rate: rate,
      subTotal: sub,
      gstRate: gstRate,
      gstAmount: gstAmt,
      lineTotal: total
    };

    setSelectedItems([...selectedItems, newItem]);
  };

  const handleQtyChange = (productId: string, qty: number) => {
    if (qty < 1) return;
    const updated = selectedItems.map(item => {
      if (item.productId === productId) {
        const sub = item.rate * qty;
        const gstAmt = Math.round(sub * item.gstRate) / 100;
        return {
          ...item,
          quantity: qty,
          subTotal: sub,
          gstAmount: gstAmt,
          lineTotal: sub + gstAmt
        };
      }
      return item;
    });
    setSelectedItems(updated);
  };

  const handleRateOverride = (productId: string, newRate: number) => {
    if (newRate < 0) return;
    const updated = selectedItems.map(item => {
      if (item.productId === productId) {
        const sub = newRate * item.quantity;
        const gstAmt = Math.round(sub * item.gstRate) / 100;
        return {
          ...item,
          rate: newRate,
          subTotal: sub,
          gstAmount: gstAmt,
          lineTotal: sub + gstAmt
        };
      }
      return item;
    });
    setSelectedItems(updated);
  };

  const handleRemoveItem = (productId: string) => {
    setSelectedItems(selectedItems.filter(item => item.productId !== productId));
  };

  // Calculations
  const subTotalSum = selectedItems.reduce((acc, item) => acc + item.subTotal, 0);
  const gstSum = selectedItems.reduce((acc, item) => acc + item.gstAmount, 0);
  const grandTotalSum = subTotalSum + gstSum;

  const nextStep = () => {
    if (currentStep === 0) {
      partyForm.validateFields().then(() => {
        if (!selectedCustomerId) {
          message.error('Please select a customer');
          return;
        }
        setCurrentStep(1);
      });
    } else if (currentStep === 1) {
      if (selectedItems.length === 0) {
        message.error('Please add at least one product to the bill');
        return;
      }
      setCurrentStep(2);
    }
  };

  const prevStep = () => {
    setCurrentStep(currentStep - 1);
  };

  const handleSubmitInvoice = async () => {
    try {
      setSubmitting(true);
      const metaValues = await metaForm.validateFields();
      
      const payload = {
        customerId: selectedCustomerId,
        invoiceDate: metaValues.invoiceDate ? metaValues.invoiceDate.toISOString() : new Date().toISOString(),
        dueDate: metaValues.dueDate ? metaValues.dueDate.toISOString() : null,
        remarks: metaValues.remarks,
        items: selectedItems.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
          overrideRate: item.rate
        }))
      };

      const res = await api.post('/invoices', payload);
      if (res.success) {
        message.success('Invoice created successfully!');
        navigate(`/invoices/${res.data.id}`);
      }
    } catch (err: any) {
      message.error(err.message || 'Failed to submit invoice');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="wizard-container">
      <Title level={3} style={{ marginBottom: 24, textAlign: 'center' }}>
        Create B2B GST Invoice (White Ledger)
      </Title>

      <Steps
        current={currentStep}
        style={{ marginBottom: 32 }}
        items={[
          { title: 'Select Party', icon: <UserOutlined /> },
          { title: 'Invoice Items', icon: <ShoppingCartOutlined /> },
          { title: 'Metadata & Submit', icon: <FileDoneOutlined /> }
        ]}
      />

      {/* Step 1: Customer Select */}
      {currentStep === 0 && (
        <Card title="Step 1: Choose Billing Party" style={{ borderRadius: 10 }}>
          <Form form={partyForm} layout="vertical">
            <Form.Item
              name="customerId"
              label="Select Billing Party (Customer)"
              rules={[{ required: true, message: 'Please select a customer profile' }]}
            >
              <Select
                showSearch
                optionFilterProp="children"
                loading={loadingCustomers}
                placeholder="Type to search party name..."
                onChange={handleCustomerChange}
                size="large"
              >
                {customers.map(c => (
                  <Option key={c.id} value={c.id}>
                    {c.partyName} {c.gstin ? `[GSTIN: ${c.gstin}]` : '[Unregistered]'}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </Form>

          {selectedCustomerId && (
            <Alert
              message="Client Catalog Loaded"
              description={`Custom pricing overrides mapped automatically. Falling back to default base price for other catalog items.`}
              type="success"
              showIcon
              style={{ marginTop: 16 }}
            />
          )}

          <div style={{ marginTop: 24, display: 'flex', justifyContent: 'flex-end' }}>
            <Button type="primary" size="large" onClick={nextStep} disabled={!selectedCustomerId}>
              Next: Add Items
            </Button>
          </div>
        </Card>
      )}

      {/* Step 2: Line Items */}
      {currentStep === 1 && (
        <Card title="Step 2: Populate Invoice Line Items" style={{ borderRadius: 10 }}>
          {/* Add product selector */}
          <div style={{ marginBottom: 24 }}>
            <Text type="secondary">Quick Add Item from Catalog: </Text>
            <Select
              showSearch
              placeholder="Search product name or model..."
              style={{ width: '100%', marginTop: 8 }}
              size="large"
              optionFilterProp="children"
              loading={loadingCatalog}
              onChange={(val) => {
                if (val) handleAddItem(val);
              }}
              value={undefined} // reset select after add
            >
              {customerCatalog.map(p => (
                <Select.Option key={p.productId} value={p.productId}>
                  {p.name} ({p.modelNumber}) — ₹{p.effectivePrice.toFixed(2)}{' '}
                  {p.hasCustomPrice ? '[Negotiated Rate]' : '[Base Price]'}
                </Select.Option>
              ))}
            </Select>
          </div>

          <Table
            dataSource={selectedItems}
            rowKey="productId"
            pagination={false}
            columns={[
              {
                title: 'Product Info',
                key: 'product',
                render: (r: SelectedItem) => (
                  <div>
                    <Text strong>{r.name}</Text>
                    <div style={{ fontSize: '11px', color: '#64748b' }}>
                      Model: {r.modelNumber} | HSN: {r.hsnCode}
                    </div>
                  </div>
                ),
              },
              {
                title: 'Qty',
                dataIndex: 'quantity',
                key: 'qty',
                width: 100,
                render: (qty: number, r: SelectedItem) => (
                  <InputNumber
                    min={1}
                    value={qty}
                    onChange={(val) => handleQtyChange(r.productId, val || 1)}
                  />
                ),
              },
              {
                title: 'Rate (₹)',
                dataIndex: 'rate',
                key: 'rate',
                width: 130,
                render: (rate: number, r: SelectedItem) => (
                  <InputNumber
                    min={0.01}
                    precision={2}
                    value={rate}
                    onChange={(val) => handleRateOverride(r.productId, val || 0.01)}
                  />
                ),
              },
              {
                title: 'Taxable (₹)',
                dataIndex: 'subTotal',
                key: 'subTotal',
                align: 'right',
                render: (sub: number) => sub.toFixed(2),
              },
              {
                title: 'GST 5% (₹)',
                dataIndex: 'gstAmount',
                key: 'gst',
                align: 'right',
                render: (gst: number) => (
                  <span>
                    {gst.toFixed(2)}
                    <div style={{ fontSize: '10px', color: '#94a3b8' }}>
                      (CGST 2.5%: {(gst / 2).toFixed(2)})
                    </div>
                  </span>
                ),
              },
              {
                title: 'Total (₹)',
                dataIndex: 'lineTotal',
                key: 'total',
                align: 'right',
                render: (val: number) => <Text strong>{val.toFixed(2)}</Text>,
              },
              {
                title: '',
                key: 'action',
                width: 60,
                render: (r: SelectedItem) => (
                  <Button danger shape="circle" icon={<DeleteOutlined />} onClick={() => handleRemoveItem(r.productId)} />
                ),
              },
            ]}
          />

          <Divider />

          {/* Totals Box */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', textAlign: 'right' }}>
            <Space direction="vertical" size="small">
              <div>
                <Text type="secondary">Taxable SubTotal: </Text>
                <Text strong style={{ fontSize: 16 }}>₹{subTotalSum.toFixed(2)}</Text>
              </div>
              <div>
                <Text type="secondary">Total GST (5%): </Text>
                <Text strong style={{ fontSize: 16 }}>₹{gstSum.toFixed(2)}</Text>
              </div>
              <Divider style={{ margin: '8px 0' }} />
              <div>
                <Text type="secondary" style={{ fontSize: 16 }}>Grand Total (Round): </Text>
                <Text strong style={{ fontSize: 22, color: '#1677ff' }}>
                  ₹{Math.round(grandTotalSum).toFixed(2)}
                </Text>
              </div>
            </Space>
          </div>

          <div style={{ marginTop: 24, display: 'flex', justifyContent: 'space-between' }}>
            <Button size="large" onClick={prevStep}>
              Back: Edit Party
            </Button>
            <Button type="primary" size="large" onClick={nextStep} disabled={selectedItems.length === 0}>
              Next: Review & Submit
            </Button>
          </div>
        </Card>
      )}

      {/* Step 3: Metadata & Submit */}
      {currentStep === 2 && (
        <Card title="Step 3: Review Invoice Details" style={{ borderRadius: 10 }}>
          <Form form={metaForm} layout="vertical">
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
              <Form.Item
                name="invoiceDate"
                label="Invoice Date"
                initialValue={dayjs()}
                rules={[{ required: true, message: 'Please select invoice date' }]}
              >
                <DatePicker style={{ width: '100%' }} format="DD MMM YYYY" />
              </Form.Item>
              <Form.Item name="dueDate" label="Due Date (Optional)">
                <DatePicker style={{ width: '100%' }} format="DD MMM YYYY" />
              </Form.Item>
            </div>
            <Form.Item name="remarks" label="Remarks / Transport Terms / Notes">
              <Input.TextArea placeholder="e.g. Terms: Goods once sold will not be returned. Transport via Gati Cargo." />
            </Form.Item>
          </Form>

          <Divider />

          {/* Quick summary check */}
          <div style={{ background: '#f8fafc', padding: 16, borderRadius: 8, marginBottom: 24 }}>
            <Title level={5} style={{ margin: 0, marginBottom: 8 }}>Invoicing Summary</Title>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
              <div><Text type="secondary">Customer Items: </Text> <Text strong>{selectedItems.length} products</Text></div>
              <div><Text type="secondary">Net Amount: </Text> <Text strong>₹{subTotalSum.toFixed(2)}</Text></div>
              <div><Text type="secondary">GST Amount: </Text> <Text strong>₹{gstSum.toFixed(2)}</Text></div>
              <div><Text type="secondary">Bill Total: </Text> <Text strong style={{ color: '#10b981' }}>₹{Math.round(grandTotalSum).toFixed(2)}</Text></div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <Button size="large" onClick={prevStep}>
              Back: Edit Items
            </Button>
            <Button
              type="primary"
              size="large"
              icon={<CreditCardOutlined />}
              onClick={handleSubmitInvoice}
              loading={submitting}
            >
              Generate Bill & Create PDF
            </Button>
          </div>
        </Card>
      )}
    </div>
  );
};

export default NewBill;
