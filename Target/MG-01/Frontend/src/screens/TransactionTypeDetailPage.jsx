import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useFormField } from '../hooks/useFormField.js';
import Alert from '../components/Alert.jsx';
import ConfirmDialog from '../components/ConfirmDialog.jsx';
import TextField from '../components/TextField.jsx';
import { transactionTypesApi } from '../api/transactionTypesApi.js';
import {
  isUnchanged, MAX_DESCRIPTION, normalizeCode, validateDescription, validateTypeCode,
} from '../validation/transactionType.js';

/** S3 – look up, view, update and delete one transaction type (legacy COTRTUPC). */
export default function TransactionTypeDetailPage() {
  const { typeCode: rawCode } = useParams();
  const navigate = useNavigate();
  const code = normalizeCode(rawCode);

  const [state, setState] = useState('loading'); // loading | viewing | notFound | error
  const [record, setRecord] = useState(null);
  const descriptionField = useFormField('', validateDescription, { clearOnChange: true });
  const { value: description, setValue: setDescription } = descriptionField;
  const [alert, setAlert] = useState(null);
  const [status, setStatus] = useState(null);
  const [confirm, setConfirm] = useState(null); // 'save' | 'delete'
  const [busy, setBusy] = useState(false);
  const [lookup, setLookup] = useState('');
  const [lookupError, setLookupError] = useState(null);
  const descriptionRef = useRef(null);

  const load = useCallback(async () => {
    setState('loading');
    setAlert(null);
    setStatus(null);
    const codeError = validateTypeCode(rawCode);
    if (codeError) {
      setAlert(codeError);
      setState('error');
      return;
    }
    try {
      const data = await transactionTypesApi.get(code);
      setRecord(data);
      setDescription(data.description);
      setState('viewing');
    } catch (err) {
      if (err.code === 'TYPE_NOT_FOUND') setState('notFound');
      else {
        setAlert(err.message);
        setState('error');
      }
    }
  }, [code, rawCode, setDescription]);

  useEffect(() => {
    load();
  }, [load]);

  const unchanged = record && isUnchanged(description, record.description);

  const onSave = (e) => {
    e.preventDefault();
    setStatus(null);
    if (descriptionField.validate()) {
      descriptionRef.current?.focus();
      return;
    }
    setConfirm('save');
  };

  const save = async () => {
    setBusy(true);
    try {
      const updated = await transactionTypesApi.update(code, description.trim());
      setRecord(updated);
      setDescription(updated.description);
      setStatus('Changes saved.');
      setAlert(null);
    } catch (err) {
      if (err.errors?.description) descriptionField.setError(err.errors.description[0]);
      else setAlert(err.message);
    } finally {
      setBusy(false);
      setConfirm(null);
    }
  };

  const remove = async () => {
    setBusy(true);
    try {
      await transactionTypesApi.remove(code);
      navigate('/transaction-types', { state: { status: `Transaction type ${code} deleted.` } });
    } catch (err) {
      setAlert(err.message);
      setConfirm(null);
    } finally {
      setBusy(false);
    }
  };

  const onLookup = (e) => {
    e.preventDefault();
    const err = validateTypeCode(lookup);
    setLookupError(err);
    if (!err) {
      navigate(`/transaction-types/${normalizeCode(lookup)}`);
      setLookup('');
    }
  };

  return (
    <div className="flex flex-col gap-sp-5">
      <div>
        <Link className="btn btn-ghost btn-sm px-0" to="/transaction-types">← Back to list</Link>
        <h1 className="m-0 text-xl text-primary-900">Transaction type {code}</h1>
      </div>

      <form className="card max-w-form" onSubmit={onLookup} noValidate aria-label="Look up another code">
        <div className="card-body flex flex-wrap items-start gap-sp-3">
          <div className="min-w-[12rem] flex-1">
            <TextField
              id="lookup-code"
              label="Look up another code"
              value={lookup}
              onChange={setLookup}
              maxLength={2}
              inputMode="numeric"
              mono
              error={lookupError}
            />
          </div>
          <button type="submit" className="btn btn-secondary md:mt-[1.85rem]">Look up</button>
        </div>
      </form>

      {state === 'loading' && <p className="text-muted" role="status">Loading…</p>}

      {state === 'error' && <Alert variant="danger">{alert}</Alert>}

      {state === 'notFound' && (
        <div className="card max-w-form">
          <div className="card-body flex flex-col gap-sp-4">
            <Alert variant="danger">{`No transaction type with code ${code}.`}</Alert>
            <div className="flex justify-end gap-sp-3">
              <Link className="btn btn-secondary" to="/transaction-types">Back to list</Link>
              <Link className="btn btn-primary" to={`/transaction-types/new?typeCode=${code}`}>Create it</Link>
            </div>
          </div>
        </div>
      )}

      {state === 'viewing' && record && (
        <form className="card max-w-form" onSubmit={onSave} noValidate aria-labelledby="detail-heading">
          <div className="card-header">
            <h2 id="detail-heading" className="m-0 text-md">Details</h2>
          </div>
          <div className="card-body">
            <Alert variant="success">{status}</Alert>
            <Alert variant="danger">{alert}</Alert>
            <TextField id="detail-type-code" label="Type code" value={record.typeCode} readOnly mono />
            <TextField
              ref={descriptionRef}
              id="detail-description"
              label="Description"
              required
              value={description}
              onChange={descriptionField.onChange}
              onBlur={descriptionField.onBlur}
              maxLength={MAX_DESCRIPTION}
              error={descriptionField.error}
              hint={unchanged ? 'No changes to save.' : `Letters, numbers and spaces. ${description.length}/${MAX_DESCRIPTION}`}
            />
          </div>
          <div className="card-footer flex flex-wrap justify-between gap-sp-3">
            <button type="button" className="btn btn-danger" onClick={() => setConfirm('delete')} disabled={busy}>
              Delete
            </button>
            <span className="flex gap-sp-3">
              <Link className="btn btn-secondary" to="/transaction-types">Back to list</Link>
              <button type="submit" className="btn btn-primary" disabled={busy || unchanged}>Save changes</button>
            </span>
          </div>
        </form>
      )}

      {confirm === 'save' && (
        <ConfirmDialog
          title="Save changes?"
          message={`Change the description of transaction type ${code} to “${description.trim()}”?`}
          confirmLabel="Save changes"
          busy={busy}
          onConfirm={save}
          onCancel={() => {
            setConfirm(null);
            setStatus('Nothing was changed.');
          }}
        />
      )}
      {confirm === 'delete' && (
        <ConfirmDialog
          title="Delete transaction type?"
          message={`Delete transaction type ${code} – ${record?.description}? This cannot be undone.`}
          confirmLabel="Delete"
          danger
          busy={busy}
          onConfirm={remove}
          onCancel={() => {
            setConfirm(null);
            setStatus('Nothing was changed.');
          }}
        />
      )}
    </div>
  );
}
