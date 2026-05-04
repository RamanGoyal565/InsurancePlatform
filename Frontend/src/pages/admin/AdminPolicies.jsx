import { useState } from 'react';
import {
  Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent,
  DialogTitle, Grid, IconButton, Table, TableBody, TableCell, TableHead,
  TableRow, TextField, Typography, MenuItem,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { usePolicies, useCreatePolicy, useUpdatePolicy } from '../../hooks/usePolicies';

const VEHICLE_TYPES = [
  { value: 1, label: 'Car' },
  { value: 2, label: 'Truck' },
  { value: 3, label: 'Bike' },
];

const schema = yup.object({
  name: yup.string().required('Name is required'),
  vehicleType: yup.number().required('Vehicle type is required'),
  premium: yup.number().positive().required('Premium is required'),
  coverageDetails: yup.string().required('Coverage details required'),
  terms: yup.string().required('Terms required'),
  policyDocument: yup.string(),
});

export default function AdminPolicies() {
  const { data: policies = [], isLoading } = usePolicies();
  const createPolicy = useCreatePolicy();
  const updatePolicy = useUpdatePolicy();

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);

  const { register, handleSubmit, reset, control, formState: { errors, isSubmitting } } = useForm({
    resolver: yupResolver(schema),
  });

  const openCreate = () => { setEditing(null); reset({}); setOpen(true); };
  const openEdit = (p) => {
    setEditing(p);
    // Do NOT populate policyDocument — it's a large base64 PDF.
    // The backend regenerates it automatically from the updated fields.
    reset({ name: p.name, vehicleType: p.vehicleType, premium: p.premium, coverageDetails: p.coverageDetails, terms: p.terms, policyDocument: '' });
    setOpen(true);
  };

  const onSubmit = async (data) => {
    if (editing) await updatePolicy.mutateAsync({ policyId: editing.policyId, ...data });
    else await createPolicy.mutateAsync(data);
    setOpen(false);
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Policies</Typography>
          <Typography variant="body2" color="text.secondary">Manage insurance policy catalog</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>New Policy</Button>
      </Box>

      <Card>
        <CardContent>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Policy Name</TableCell>
                <TableCell>Vehicle Type</TableCell>
                <TableCell>Premium (₹/yr)</TableCell>
                <TableCell>Coverage</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={5} align="center">Loading...</TableCell></TableRow>
              ) : policies.map((p) => (
                <TableRow key={p.policyId} hover>
                  <TableCell><Typography fontWeight={600}>{p.name}</Typography></TableCell>
                  <TableCell>
                    <Chip label={VEHICLE_TYPES.find((v) => v.value === p.vehicleType)?.label || p.vehicleType} size="small" />
                  </TableCell>
                  <TableCell>₹{p.premium?.toLocaleString()}</TableCell>
                  <TableCell sx={{ maxWidth: 200 }}>
                    <Typography variant="body2" noWrap>{p.coverageDetails}</Typography>
                  </TableCell>
                  <TableCell>
                    <IconButton size="small" onClick={() => openEdit(p)}><EditIcon /></IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editing ? 'Edit Policy' : 'Create Policy'}</DialogTitle>
        <Box component="form" onSubmit={handleSubmit(onSubmit)}>
          <DialogContent>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField label="Policy Name" fullWidth {...register('name')} error={!!errors.name} helperText={errors.name?.message} />
              </Grid>
              <Grid item xs={6}>
                <Controller
                  name="vehicleType"
                  control={control}
                  render={({ field }) => (
                    <TextField select label="Vehicle Type" fullWidth {...field} error={!!errors.vehicleType} helperText={errors.vehicleType?.message}>
                      {VEHICLE_TYPES.map((v) => <MenuItem key={v.value} value={v.value}>{v.label}</MenuItem>)}
                    </TextField>
                  )}
                />
              </Grid>
              <Grid item xs={6}>
                <TextField label="Premium (₹/year)" type="number" fullWidth {...register('premium')} error={!!errors.premium} helperText={errors.premium?.message} />
              </Grid>
              <Grid item xs={12}>
                <TextField label="Coverage Details" fullWidth multiline rows={3} {...register('coverageDetails')} error={!!errors.coverageDetails} helperText={errors.coverageDetails?.message || 'Separate items with a semicolon (;) e.g. Own damage; Theft; Fire'} />
              </Grid>
              <Grid item xs={12}>
                <TextField label="Terms & Conditions" fullWidth multiline rows={3} {...register('terms')} error={!!errors.terms} helperText={errors.terms?.message} />
              </Grid>
              <Grid item xs={12}>
                <TextField label="Policy Document (URL or text)" fullWidth {...register('policyDocument')} />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>{editing ? 'Update' : 'Create'}</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  );
}
