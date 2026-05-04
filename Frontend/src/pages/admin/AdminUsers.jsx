import { useState } from 'react';
import {
  Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent,
  DialogTitle, Grid, IconButton, Table, TableBody, TableCell, TableHead,
  TableRow, TextField, Typography, ToggleButton, ToggleButtonGroup,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import BlockIcon from '@mui/icons-material/Block';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useUsers, useCreateClaimsSpecialist, useCreateSupportSpecialist, useUpdateUserStatus } from '../../hooks/useUsers';

const schema = yup.object({
  name: yup.string().min(2).required('Name is required'),
  email: yup.string().email().required('Email is required'),
  password: yup.string().min(8).required('Password is required (min 8 chars)'),
});

const ROLE_COLORS = {
  Admin: 'error',
  Customer: 'primary',
  ClaimsSpecialist: 'warning',
  SupportSpecialist: 'info',
};

export default function AdminUsers() {
  const { data: users = [], isLoading } = useUsers();
  const createClaims = useCreateClaimsSpecialist();
  const createSupport = useCreateSupportSpecialist();
  const updateStatus = useUpdateUserStatus();

  const [open, setOpen] = useState(false);
  const [roleType, setRoleType] = useState('ClaimsSpecialist');
  const [search, setSearch] = useState('');

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm({ resolver: yupResolver(schema) });

  const onSubmit = async (data) => {
    if (roleType === 'ClaimsSpecialist') await createClaims.mutateAsync(data);
    else await createSupport.mutateAsync(data);
    reset();
    setOpen(false);
  };

  const filtered = users.filter((u) =>
    !search || u.name?.toLowerCase().includes(search.toLowerCase()) ||
    u.email?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Users & Roles</Typography>
          <Typography variant="body2" color="text.secondary">Manage platform users and their roles</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          Add Specialist
        </Button>
      </Box>

      <Card>
        <CardContent>
          <TextField
            placeholder="Search users..."
            size="small"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            sx={{ mb: 2, width: 300 }}
          />
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Role</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={6} align="center">Loading...</TableCell></TableRow>
              ) : filtered.map((u) => (
                <TableRow key={u.userId} hover>
                  <TableCell>{u.name}</TableCell>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>
                    <Chip label={u.role} color={ROLE_COLORS[u.role] || 'default'} size="small" />
                  </TableCell>
                  <TableCell>
                    <Chip label={u.isActive ? 'Active' : 'Inactive'} color={u.isActive ? 'success' : 'default'} size="small" />
                  </TableCell>
                  <TableCell>{new Date(u.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <IconButton
                      size="small"
                      color={u.isActive ? 'error' : 'success'}
                      onClick={() => updateStatus.mutate({ userId: u.userId, isActive: !u.isActive })}
                      title={u.isActive ? 'Deactivate' : 'Activate'}
                    >
                      {u.isActive ? <BlockIcon /> : <CheckCircleIcon />}
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Create Specialist Dialog */}
      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add New Specialist</DialogTitle>
        <Box component="form" onSubmit={handleSubmit(onSubmit)}>
          <DialogContent>
            <ToggleButtonGroup
              value={roleType}
              exclusive
              onChange={(_, v) => v && setRoleType(v)}
              sx={{ mb: 2 }}
            >
              <ToggleButton value="ClaimsSpecialist">Claims Specialist</ToggleButton>
              <ToggleButton value="SupportSpecialist">Support Specialist</ToggleButton>
            </ToggleButtonGroup>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField label="Full Name" fullWidth {...register('name')} error={!!errors.name} helperText={errors.name?.message} />
              </Grid>
              <Grid item xs={12}>
                <TextField label="Email" fullWidth {...register('email')} error={!!errors.email} helperText={errors.email?.message} />
              </Grid>
              <Grid item xs={12}>
                <TextField label="Password" type="password" fullWidth {...register('password')} error={!!errors.password} helperText={errors.password?.message} />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>Create</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  );
}
