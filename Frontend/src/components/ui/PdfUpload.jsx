import { useRef, useState } from 'react';
import PropTypes from 'prop-types';
import { Box, Button, IconButton, Typography, LinearProgress } from '@mui/material';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import CloseIcon from '@mui/icons-material/Close';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';

const MAX_BYTES = 1 * 1024 * 1024; // 1 MB

/**
 * Single-PDF upload that converts the file to base64 and calls onChange.
 * @param {{ onChange: (base64: string|null, fileName: string|null) => void, error?: string }} props
 */
export default function PdfUpload({ onChange, error }) {
  const inputRef = useRef(null);
  const [file, setFile] = useState(null);
  const [sizeError, setSizeError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleFile = (f) => {
    setSizeError('');
    if (!f) return;
    if (f.type !== 'application/pdf') {
      setSizeError('Only PDF files are allowed.');
      return;
    }
    if (f.size > MAX_BYTES) {
      setSizeError(`File too large. Max size is 1 MB (selected: ${(f.size / 1024 / 1024).toFixed(2)} MB).`);
      return;
    }
    setLoading(true);
    const reader = new FileReader();
    reader.onload = (e) => {
      // Strip the data URL prefix — send only the raw base64 string
      const base64 = e.target.result.split(',')[1];
      setFile(f);
      onChange(base64, f.name);
      setLoading(false);
    };
    reader.readAsDataURL(f);
  };

  const handleRemove = () => {
    setFile(null);
    setSizeError('');
    onChange(null, null);
    if (inputRef.current) inputRef.current.value = '';
  };

  const displayError = sizeError || error;

  return (
    <Box>
      {!file ? (
        <Box
          sx={{
            border: '1px dashed',
            borderColor: displayError ? 'error.main' : 'divider',
            borderRadius: 2,
            p: 2,
            textAlign: 'center',
            cursor: 'pointer',
            bgcolor: 'grey.50',
            '&:hover': { bgcolor: 'grey.100' },
          }}
          onClick={() => inputRef.current?.click()}
        >
          <AttachFileIcon sx={{ color: 'text.secondary', mb: 0.5 }} />
          <Typography variant="body2" color="text.secondary">
            Click to attach a PDF (max 1 MB)
          </Typography>
          <input
            ref={inputRef}
            type="file"
            accept="application/pdf"
            style={{ display: 'none' }}
            onChange={(e) => handleFile(e.target.files?.[0])}
          />
        </Box>
      ) : (
        <Box
          sx={{
            display: 'flex', alignItems: 'center', gap: 1,
            border: '1px solid', borderColor: 'success.light',
            borderRadius: 2, p: 1.5, bgcolor: 'success.50',
          }}
        >
          <PictureAsPdfIcon color="error" />
          <Typography variant="body2" sx={{ flexGrow: 1 }} noWrap>{file.name}</Typography>
          <Typography variant="caption" color="text.secondary">
            {(file.size / 1024).toFixed(0)} KB
          </Typography>
          <IconButton size="small" onClick={handleRemove}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
      )}
      {loading && <LinearProgress sx={{ mt: 0.5, borderRadius: 1 }} />}
      {displayError && (
        <Typography variant="caption" color="error" display="block" mt={0.5}>
          {displayError}
        </Typography>
      )}
    </Box>
  );
}

PdfUpload.propTypes = {
  onChange: PropTypes.func.isRequired,
  error: PropTypes.string,
};
