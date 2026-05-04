import { useState } from 'react';
import {
  Box, Button, Card, CardContent, IconButton, Table, TableBody,
  TableCell, TableHead, TableRow, Tooltip, Typography,
} from '@mui/material';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import TwoWheelerIcon from '@mui/icons-material/TwoWheeler';
import DescriptionIcon from '@mui/icons-material/Description';
import { useCustomerPolicies, useRenewPolicy } from '../../hooks/usePolicies';
import StatusChip from '../../components/ui/StatusChip';
import PolicyDocumentViewer from '../../components/ui/PolicyDocumentViewer';

const VEHICLE_ICONS = { 1: <DirectionsCarIcon />, 2: <LocalShippingIcon />, 3: <TwoWheelerIcon /> };
const VEHICLE_LABELS = { 1: 'Car', 2: 'Truck', 3: 'Bike' };
const STATUS_LABELS = ['', 'Pending', 'Active', 'Renewed', 'Cancelled', 'Expired'];

export default function MyPolicies() {
  const { data: policies = [], isLoading } = useCustomerPolicies();
  const renew = useRenewPolicy();
  const [viewDoc, setViewDoc] = useState(null); // { policyId, name }

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={0.5}>My Policies</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Manage and renew your existing policies
      </Typography>

      <Card>
        <CardContent>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Policy No.</TableCell>
                <TableCell>Policy Name</TableCell>
                <TableCell>Vehicle No.</TableCell>
                <TableCell>Driving License No.</TableCell>
                <TableCell>Vehicle Type</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Policy End Date</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">Loading...</TableCell>
                </TableRow>
              ) : policies.map((p) => (
                <TableRow key={p.customerPolicyId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>
                    POL-{p.customerPolicyId?.slice(0, 8).toUpperCase()}
                  </TableCell>
                  <TableCell>{p.policyName}</TableCell>
                  <TableCell>{p.vehicleNumber}</TableCell>
                  <TableCell>{p.drivingLicenseNumber}</TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      {VEHICLE_ICONS[p.vehicleType]}
                      <Typography variant="body2">{VEHICLE_LABELS[p.vehicleType]}</Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <StatusChip status={STATUS_LABELS[p.status] || String(p.status)} />
                  </TableCell>
                  <TableCell>{new Date(p.endDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Button
                        size="small"
                        variant={p.status === 5 ? 'contained' : 'outlined'}
                        onClick={() => renew.mutate(p.customerPolicyId)}
                        disabled={renew.isPending}
                      >
                        {p.status === 5 ? 'Buy Again' : 'Renew'}
                      </Button>
                      <Tooltip title="View / Download Policy Document">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => setViewDoc({ policyId: p.policyId, name: p.policyName })}
                        >
                          <DescriptionIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
              {policies.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                    No policies found. Browse and purchase a policy to get started.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* PDF Viewer */}
      <PolicyDocumentViewer
        policyId={viewDoc?.policyId ?? null}
        policyName={viewDoc?.name}
        onClose={() => setViewDoc(null)}
      />
    </Box>
  );
}
