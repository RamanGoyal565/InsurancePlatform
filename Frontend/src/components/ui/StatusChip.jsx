import PropTypes from 'prop-types';
import { Chip } from '@mui/material';

const COLOR_MAP = {
  // Policy statuses
  Active: 'success',
  Pending: 'warning',
  Renewed: 'info',
  Cancelled: 'error',
  Expired: 'default',
  // Ticket statuses
  Open: 'warning',
  InReview: 'info',
  Assigned: 'primary',
  Resolved: 'success',
  Rejected: 'error',
  Closed: 'default',
  // Payment statuses
  Completed: 'success',
  Failed: 'error',
  // Claim approval
  Verified: 'info',
  Approved: 'success',
  // User
  true: 'success',
  false: 'error',
};

export default function StatusChip({ status }) {
  const color = COLOR_MAP[String(status)] || 'default';
  return <Chip label={status} color={color} size="small" />;
}

StatusChip.propTypes = {
  status: PropTypes.oneOfType([PropTypes.string, PropTypes.bool]).isRequired,
};
