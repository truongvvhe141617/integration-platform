import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Plus, Trash2, Loader2, Save, AlertCircle } from 'lucide-react';
import { useCreateIntegration, useUpdateIntegration, useIntegration } from '../../hooks/useIntegrations';
import { toast } from '../../lib/toast';

export { IntegrationFormPage };
function IntegrationFormPage() { return <IntegrationForm />; }

interface Mapping { source: string; target: string; required: boolean; }
interface Operation { name: string; method: string; path: string; }
interface Errors { [key: string]: string; }

const CONFIGKEY_REGEX = /^[a-z0-9]+(-[a-z0-9]+)*$/;
const URL_REGEX = /^https?:\/\/.+/;
const HTTP_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'];

function validate(f: {
  configKey: string; name: string; baseUrl: string;
  authType: string; authToken: string; timeout: number;
  operations: Operation[]; mappings: Mapping[];
}): Errors {
  const errors: Errors = {};
  if (!f.configKey.trim()) errors.configKey = 'Config Key là bắt buộc';
  else if (!CONFIGKEY_REGEX.test(f.configKey)) errors.configKey = 'Chỉ chữ thường, số, dấu gạch ngang';
  if (!f.name.trim()) errors.name = 'Tên là bắt buộc';
  if (!f.baseUrl.trim()) errors.baseUrl = 'Base URL là bắt buộc';
  else if (!URL_REGEX.test(f.baseUrl)) errors.baseUrl = 'Phải bắt đầu bằng http:// hoặc https://';
  if (f.authType !== 'none' && !f.authToken.trim()) errors.authToken = 'Token/Key là bắt buộc';
  if (f.timeout < 1000 || f.timeout > 120000) errors.timeout = '1000 - 120000 ms';
  if (f.operations.length === 0) errors.operations = 'Cần ít nhất 1 operation';
  f.operations.forEach((op, i) => {
    if (!op.name.trim()) errors[`op_name_${i}`] = 'Tên operation bắt buộc';
    if (!op.path.trim()) errors[`op_path_${i}`] = 'Path bắt buộc';
  });
  if (f.mappings.length > 0) {
    const empty = f.mappings.filter(m => !m.source.trim() || !m.target.trim());
    if (empty.length > 0) errors.mappings = 'Mapping phải có cả Source và Target';
  }
  return errors;
}

export default function IntegrationForm() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id && id !== 'new';

  const createMutation = useCreateIntegration();
  const updateMutation = useUpdateIntegration();
  const { data: existing } = useIntegration(isEdit ? id : '');

  const [name, setName] = useState('');
  const [configKey, setConfigKey] = useState('');
  const [baseUrl, setBaseUrl] = useState('');
  const [authType, setAuthType] = useState('none');
  const [authToken, setAuthToken] = useState('');
  const [timeout, setTimeout] = useState(30000);
  const [operations, setOperations] = useState<Operation[]>([{ name: 'default', method: 'GET', path: '/' }]);
  const [mappings, setMappings] = useState<Mapping[]>([]);
  const [showMappings, setShowMappings] = useState(false);
  const [errors, setErrors] = useState<Errors>({});
  const [touched, setTouched] = useState<Set<string>>(new Set());
  const [loaded, setLoaded] = useState(false);

  // Load existing data for edit mode
  useEffect(() => {
    if (isEdit && existing && !loaded) {
      setName(existing.name || '');
      setConfigKey(existing.configKey || '');
      setBaseUrl(existing.baseUrl || '');
      setAuthType(existing.authType || 'none');
      setTimeout(existing.timeoutMs || 30000);
      if (existing.operations?.length > 0) {
        setOperations(existing.operations.map((op: any) => ({
          name: op.name || 'default',
          method: op.httpMethod || 'GET',
          path: op.path || '/',
        })));
      }
      setLoaded(true);
    }
  }, [isEdit, existing, loaded]);

  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSave = async () => {
    const errs = validate({ configKey, name, baseUrl, authType, authToken, timeout, operations, mappings });
    setErrors(errs);
    setTouched(new Set(Object.keys(errs).concat(['configKey', 'name', 'baseUrl', 'authToken', 'timeout', 'operations'])));
    if (Object.keys(errs).length > 0) { toast.error('Kiểm tra lại thông tin'); return; }

    const payload = {
      configKey: configKey.trim(),
      name: name.trim(),
      connectorType: 'generic-http',
      baseUrl: baseUrl.trim(),
      authType,
      authParams: authType !== 'none' ? JSON.stringify({ token: authToken.trim() }) : undefined,
      timeoutMs: timeout,
      operations: operations.map(op => ({
        name: op.name.trim(),
        httpMethod: op.method,
        path: op.path.trim(),
        requestMappings: mappings.filter(m => m.source && m.target).map((m, i) => ({
          sourceField: m.source.trim(), targetField: m.target.trim(), isRequired: m.required, sortOrder: i,
        })),
        responseMappings: [],
        validations: [],
      })),
    };

    try {
      if (isEdit) {
        await updateMutation.mutateAsync({ id, data: payload });
      } else {
        await createMutation.mutateAsync(payload);
      }
      navigate('/integrations');
    } catch { /* hook handles */ }
  };

  const handleNameChange = (val: string) => {
    setName(val);
    if (!isEdit && (!touched.has('configKey') || !configKey)) {
      setConfigKey(val.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''));
    }
  };

  const showError = (field: string) => touched.has(field) && errors[field];

  return (
    <div style={{ maxWidth: 680, margin: '0 auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 32 }}>
        <button className="btn btn-ghost btn-sm" onClick={() => navigate('/integrations')}><ArrowLeft size={16} /></button>
        <div>
          <h1 style={{ fontSize: 20, fontWeight: 600, margin: 0 }}>{isEdit ? 'Edit Integration' : 'New Integration'}</h1>
          <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0 }}>
            {isEdit ? `Editing: ${configKey}` : 'Cấu hình kết nối API đối tác'}
          </p>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        {/* Basic */}
        <Section title="Thông tin cơ bản">
          <Field label="Tên" required error={showError('name')}>
            <input className="input" value={name} onChange={e => handleNameChange(e.target.value)}
              onBlur={() => touched.add('name')} placeholder="VD: JSONPlaceholder Users" />
          </Field>
          <Field label="Config Key" required error={showError('configKey')} hint="ID duy nhất dùng khi gọi execute">
            <input className="input" value={configKey} disabled={isEdit}
              onChange={e => { setConfigKey(e.target.value); touched.add('configKey'); }}
              placeholder="jsonplaceholder" style={{ fontFamily: 'var(--font-mono)' }} />
          </Field>
          <Field label="Base URL" required error={showError('baseUrl')} hint="Domain gốc, KHÔNG bao gồm path">
            <input className="input" value={baseUrl} onChange={e => setBaseUrl(e.target.value)}
              onBlur={() => touched.add('baseUrl')}
              placeholder="https://jsonplaceholder.typicode.com" style={{ fontFamily: 'var(--font-mono)' }} />
          </Field>
        </Section>

        {/* Operations */}
        <Section title="Operations">
          <div style={{ fontSize: 12, color: 'var(--color-text-tertiary)', marginBottom: 12 }}>
            Mỗi operation = 1 endpoint cụ thể. VD: GET /users, POST /transfer
          </div>
          {showError('operations') && (
            <div style={{ fontSize: 12, color: 'var(--color-danger)', marginBottom: 8 }}><AlertCircle size={12} style={{ display: 'inline' }} /> {errors.operations}</div>
          )}
          {operations.map((op, i) => (
            <div key={i} style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 10, padding: 12, background: 'var(--color-bg-subtle)', borderRadius: 8 }}>
              <input className="input" value={op.name} placeholder="tên operation"
                onChange={e => { const c = [...operations]; c[i].name = e.target.value; setOperations(c); }}
                style={{ width: 120, fontSize: 12, fontFamily: 'var(--font-mono)' }} />
              <select className="input" value={op.method} style={{ width: 90, fontSize: 12 }}
                onChange={e => { const c = [...operations]; c[i].method = e.target.value; setOperations(c); }}>
                {HTTP_METHODS.map(m => <option key={m} value={m}>{m}</option>)}
              </select>
              <input className="input" value={op.path} placeholder="/users"
                onChange={e => { const c = [...operations]; c[i].path = e.target.value; setOperations(c); }}
                style={{ flex: 1, fontSize: 12, fontFamily: 'var(--font-mono)' }} />
              {operations.length > 1 && (
                <button className="btn btn-ghost btn-sm" onClick={() => setOperations(operations.filter((_, j) => j !== i))}
                  style={{ color: 'var(--color-danger)', padding: 4 }}><Trash2 size={13} /></button>
              )}
            </div>
          ))}
          <button className="btn btn-ghost btn-sm" onClick={() => setOperations([...operations, { name: '', method: 'GET', path: '' }])}>
            <Plus size={13} /> Thêm operation
          </button>
        </Section>

        {/* Auth */}
        <Section title="Xác thực">
          <Field label="Phương thức">
            <select className="input" value={authType} onChange={e => setAuthType(e.target.value)}>
              <option value="none">Không (None)</option>
              <option value="apikey">API Key</option>
              <option value="basic">Basic Auth</option>
              <option value="bearer">Bearer Token</option>
            </select>
          </Field>
          {authType !== 'none' && (
            <Field label="Token / Key" required error={showError('authToken')}>
              <input className="input" type="password" value={authToken}
                onChange={e => setAuthToken(e.target.value)} placeholder="sk-xxxx..."
                style={{ fontFamily: 'var(--font-mono)' }} />
            </Field>
          )}
          <Field label="Timeout (ms)" error={showError('timeout')}>
            <input className="input" type="number" value={timeout}
              onChange={e => setTimeout(Number(e.target.value))} style={{ width: 120 }} />
          </Field>
        </Section>

        {/* Mappings */}
        <Section title="Field Mapping (tùy chọn)">
          <div style={{ fontSize: 12, color: 'var(--color-text-tertiary)', marginBottom: 8 }}>
            Đổi tên field trước khi gửi. Bỏ qua nếu không cần.
          </div>
          {!showMappings && mappings.length === 0 ? (
            <button className="btn btn-outline btn-sm" onClick={() => setShowMappings(true)}>
              <Plus size={13} /> Thêm mapping
            </button>
          ) : (
            <>
              {mappings.map((m, i) => (
                <div key={i} style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 8 }}>
                  <input className="input" value={m.source} placeholder="source"
                    onChange={e => { const c = [...mappings]; c[i].source = e.target.value; setMappings(c); }}
                    style={{ flex: 1, fontFamily: 'var(--font-mono)', fontSize: 12 }} />
                  <span style={{ color: 'var(--color-text-tertiary)' }}>→</span>
                  <input className="input" value={m.target} placeholder="target"
                    onChange={e => { const c = [...mappings]; c[i].target = e.target.value; setMappings(c); }}
                    style={{ flex: 1, fontFamily: 'var(--font-mono)', fontSize: 12 }} />
                  <button className="btn btn-ghost btn-sm" onClick={() => setMappings(mappings.filter((_, j) => j !== i))}
                    style={{ color: 'var(--color-danger)', padding: 4 }}><Trash2 size={13} /></button>
                </div>
              ))}
              <button className="btn btn-ghost btn-sm" onClick={() => setMappings([...mappings, { source: '', target: '', required: false }])}>
                <Plus size={13} /> Thêm
              </button>
            </>
          )}
        </Section>

        {/* Save */}
        <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end', paddingTop: 8, borderTop: '1px solid var(--color-border)' }}>
          <button className="btn btn-ghost btn-md" onClick={() => navigate('/integrations')}>Hủy</button>
          <button className="btn btn-primary btn-md" onClick={handleSave} disabled={isPending}>
            {isPending ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
            {isPending ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
          </button>
        </div>
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="card" style={{ padding: 20 }}>
      <h3 style={{ fontSize: 14, fontWeight: 600, margin: '0 0 14px 0' }}>{title}</h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>{children}</div>
    </div>
  );
}

function Field({ label, required, error, hint, children }: {
  label: string; required?: boolean; error?: string | false; hint?: string; children: React.ReactNode;
}) {
  return (
    <div>
      <label style={{ display: 'block', fontSize: 12, fontWeight: 500, color: 'var(--color-text-secondary)', marginBottom: 5 }}>
        {label}{required && <span style={{ color: 'var(--color-danger)' }}> *</span>}
      </label>
      {children}
      {error && <div style={{ marginTop: 4, fontSize: 11, color: 'var(--color-danger)', display: 'flex', alignItems: 'center', gap: 4 }}><AlertCircle size={11} />{error}</div>}
      {hint && !error && <div style={{ marginTop: 3, fontSize: 11, color: 'var(--color-text-tertiary)' }}>{hint}</div>}
    </div>
  );
}
