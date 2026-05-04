import { useEffect, useMemo } from 'react';
import PropTypes from 'prop-types';
import {
  Box, Button, Dialog, DialogTitle, IconButton, Tooltip, Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';

/**
 * Lightweight PDF viewer for base64 data already in memory.
 * Used for ticket/comment attachments.
 */
export default function Base64PdfViewer({ open, base64, fileName, onClose }) {
  const blobUrl = useMemo(() => {
    if (!base64) return null;
    try {
      const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
      const blob = new Blob([bytes], { type: 'application/pdf' });
      return URL.createObjectURL(blob);
    } catch {
      return null;
    }
  }, [base64]);

  useEffect(() => {
    return () => { if (blobUrl) URL.revokeObjectURL(blobUrl); };
  }, [blobUrl]);

  const handleDownload = () => {
    if (!blobUrl) return;
    const a = document.createElement('a');
    a.href = blobUrl;
    a.download = fileName || 'attachment.pdf';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  return (
    <Dialog open={open} onClose={onClose} fullScreen PaperProps={{ sx: { bgcolor: '#1a1a2e' } }}>
      <DialogTitle
        sx={{
          display: 'flex', alignItems: 'center', gap: 1,
          bgcolor: '#0f3460', color: '#fff', py: 1.5, px: 2,
        }}
      >
        <Typography variant="subtitle1" fontWeight={600} sx={{ flexGrow: 1 }} noWrap>
          {fileName || 'Attachment'}
        </Typography>
        <Tooltip title="Download">
          <Button
            variant="contained"
            size="small"
            startIcon={<DownloadIcon />}
            onClick={handleDownload}
            disabled={!blobUrl}
            sx={{ bgcolor: '#1565C0', mr: 1 }}
          >
            Download
          </Button>
        </Tooltip>
        <Tooltip title="Close">
          <IconButton onClick={onClose} sx={{ color: '#fff' }}>
            <CloseIcon />
          </IconButton>
        </Tooltip>
      </DialogTitle>

      <Box sx={{ flexGrow: 1, height: 'calc(100vh - 64px)' }}>
        {blobUrl ? (
          <iframe
            src={blobUrl}
            title={fileName || 'PDF'}
            style={{ width: '100%', height: '100%', border: 'none' }}
          />
        ) : (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            <Typography color="grey.400">Document could not be loaded.</Typography>
          </Box>
        )}
      </Box>
    </Dialog>
  );
}

Base64PdfViewer.propTypes = {
  open: PropTypes.bool.isRequired,
  base64: PropTypes.string,
  fileName: PropTypes.string,
  onClose: PropTypes.func.isRequired,
};
