import {
  Avatar,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
  EditIcon,
  DeleteIcon,
  AddIcon,
} from '@/components/ui';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller, type SubmitHandler } from 'react-hook-form';
import { usersApi } from '../../shared/api/users';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { UserRole } from '../../shared/types';
import { useToast } from '../../shared/hooks/useToast';
import type { UserDto } from '../../shared/types';

const ROLE_OPTIONS = [
  { value: UserRole.Employee, label: 'Employee' },
  { value: UserRole.Contributor, label: 'Contributor' },
  { value: UserRole.Manager, label: 'Manager' },
  { value: UserRole.KnowledgeTeam, label: 'Knowledge Team' },
  { value: UserRole.Admin, label: 'Admin' },
  { value: UserRole.SuperAdmin, label: 'Super Admin' },
];

function getRoleLabel(role: number): string {
  return ROLE_OPTIONS.find((r) => r.value === role)?.label ?? `Role(${role})`;
}

// ── Create form ─────────────────────────────────────────────────────────────
interface CreateFormValues {
  fullName: string;
  email: string;
  password: string;
  role: number;
  department: string;
  designation: string;
}

function CreateUserDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const { register, handleSubmit, control, reset, formState: { errors } } = useForm<CreateFormValues>({
    defaultValues: { fullName: '', email: '', password: '', role: UserRole.Employee, department: '', designation: '' },
  });

  const mutation = useMutation({
    mutationFn: (v: CreateFormValues) =>
      usersApi.createUser({
        fullName: v.fullName,
        email: v.email,
        password: v.password,
        role: v.role,
        department: v.department || undefined,
        designation: v.designation || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      reset();
      onClose();
      toast.success('User created successfully.');
    },
    onError: () => toast.error('Failed to create user.'),
  });

  const onSubmit: SubmitHandler<CreateFormValues> = (v) => mutation.mutate(v);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Add New User</DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent>
          <Stack spacing={2.5} mt={1}>
            <ApiErrorAlert error={mutation.error} />
            <TextField
              label="Full Name"
              fullWidth
              required
              error={!!errors.fullName}
              {...register('fullName', { required: 'Required' })}
            />
            <TextField
              label="Email"
              fullWidth
              required
              type="email"
              error={!!errors.email}
              {...register('email', { required: 'Required' })}
            />
            <TextField
              label="Initial Password"
              fullWidth
              required
              type="password"
              error={!!errors.password}
              helperText="Minimum 6 characters"
              {...register('password', { required: 'Required', minLength: { value: 6, message: 'Min 6 chars' } })}
            />
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel>Role</InputLabel>
                  <Select {...field} label="Role">
                    {ROLE_OPTIONS.map((r) => (
                      <MenuItem key={r.value} value={r.value}>{r.label}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />
            <TextField label="Department" fullWidth {...register('department')} />
            <TextField label="Designation" fullWidth {...register('designation')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>
            Create User
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Edit form ────────────────────────────────────────────────────────────────
interface EditFormValues {
  fullName: string;
  email: string;
  role: number;
  isActive: boolean;
  department: string;
  designation: string;
}

function EditUserDialog({ user, onClose }: { user: UserDto; onClose: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const { register, handleSubmit, control, formState: { errors } } = useForm<EditFormValues>({
    defaultValues: {
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      isActive: user.isActive,
      department: user.department ?? '',
      designation: user.designation ?? '',
    },
  });

  const mutation = useMutation({
    mutationFn: (v: EditFormValues) =>
      usersApi.adminUpdateUser(user.id, {
        fullName: v.fullName,
        email: v.email,
        role: v.role,
        isActive: v.isActive,
        department: v.department || undefined,
        designation: v.designation || undefined,
        recordVersion: user.recordVersion,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      onClose();
      toast.success('User updated successfully.');
    },
    onError: () => toast.error('Failed to update user.'),
  });

  const onSubmit: SubmitHandler<EditFormValues> = (v) => mutation.mutate(v);

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit User — {user.fullName}</DialogTitle>
      <form onSubmit={handleSubmit(onSubmit)}>
        <DialogContent>
          <Stack spacing={2.5} mt={1}>
            <ApiErrorAlert error={mutation.error} />
            <TextField
              label="Full Name"
              fullWidth
              required
              error={!!errors.fullName}
              {...register('fullName', { required: 'Required' })}
            />
            <TextField
              label="Email"
              fullWidth
              required
              type="email"
              error={!!errors.email}
              {...register('email', { required: 'Required' })}
            />
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel>Role</InputLabel>
                  <Select {...field} label="Role">
                    {ROLE_OPTIONS.map((r) => (
                      <MenuItem key={r.value} value={r.value}>{r.label}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />
            <Controller
              name="isActive"
              control={control}
              render={({ field }) => (
                <FormControl fullWidth>
                  <InputLabel>Status</InputLabel>
                  <Select
                    value={field.value ? 'true' : 'false'}
                    onChange={(e) => field.onChange(e.target.value === 'true')}
                    label="Status"
                  >
                    <MenuItem value="true">Active</MenuItem>
                    <MenuItem value="false">Inactive</MenuItem>
                  </Select>
                </FormControl>
              )}
            />
            <TextField label="Department" fullWidth {...register('department')} />
            <TextField label="Designation" fullWidth {...register('designation')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={mutation.isPending}>
            Save Changes
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────
export default function UserManagementPage() {
  usePageTitle('User Management');
  const { user: currentUser } = useAuth();
  const qc = useQueryClient();

  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<number | ''>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const [createOpen, setCreateOpen] = useState(false);
  const [editUser, setEditUser] = useState<UserDto | null>(null);
  const [deactivateTarget, setDeactivateTarget] = useState<UserDto | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['admin-users', search, roleFilter, statusFilter, page],
    queryFn: ({ signal }) =>
      usersApi.getUsers({
        search: search || undefined,
        role: roleFilter !== '' ? (roleFilter as number) : undefined,
        isActive: statusFilter === '' ? undefined : statusFilter === 'active',
        pageNumber: page,
        pageSize: PAGE_SIZE,
      }, signal),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => usersApi.deactivateUser(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      setDeactivateTarget(null);
    },
  });

  return (
    <Box>
      <PageHeader
        title="User Management"
        subtitle="Create, edit, and manage user accounts"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Add User
          </Button>
        }
      />

      {/* Filters */}
      <Stack direction="row" spacing={2} mb={3} flexWrap="wrap">
        <TextField
          size="small"
          label="Search name or email"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          sx={{ minWidth: 240 }}
        />
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Role</InputLabel>
          <Select
            value={roleFilter}
            onChange={(e) => { setRoleFilter(e.target.value as number | ''); setPage(1); }}
            label="Role"
          >
            <MenuItem value="">All roles</MenuItem>
            {ROLE_OPTIONS.map((r) => (
              <MenuItem key={r.value} value={r.value}>{r.label}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 140 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
            label="Status"
          >
            <MenuItem value="">All</MenuItem>
            <MenuItem value="active">Active</MenuItem>
            <MenuItem value="inactive">Inactive</MenuItem>
          </Select>
        </FormControl>
      </Stack>

      <ApiErrorAlert error={error} />

      {isLoading ? (
        <LoadingOverlay />
      ) : (
        <>
          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>User</TableCell>
                  <TableCell>Department</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.data.map((u) => (
                  <TableRow key={u.id} hover>
                    <TableCell>
                      <Box display="flex" alignItems="center" gap={1.5}>
                        <Avatar src={u.profilePhotoUrl ?? undefined} sx={{ width: 36, height: 36, fontSize: 14 }}>
                          {u.fullName[0]}
                        </Avatar>
                        <Box>
                          <Typography variant="body2" fontWeight={600}>{u.fullName}</Typography>
                          <Typography variant="caption" color="text.secondary">{u.email}</Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{u.department ?? '—'}</Typography>
                      {u.designation && (
                        <Typography variant="caption" color="text.secondary">{u.designation}</Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Chip label={getRoleLabel(u.role)} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={u.isActive ? 'Active' : 'Inactive'}
                        size="small"
                        color={u.isActive ? 'success' : 'default'}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                        <Button
                          size="small"
                          startIcon={<EditIcon />}
                          onClick={() => setEditUser(u)}
                        >
                          Edit
                        </Button>
                        {u.id !== currentUser?.userId && u.isActive && (
                          <Button
                            size="small"
                            color="error"
                            startIcon={<DeleteIcon />}
                            onClick={() => setDeactivateTarget(u)}
                          >
                            Deactivate
                          </Button>
                        )}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.data.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 4 }}>
                      <Typography color="text.secondary">No users found.</Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Box>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <Box display="flex" justifyContent="center" mt={3} gap={1} alignItems="center">
              <Button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Typography variant="body2">
                Page {page} of {data.totalPages} &nbsp;·&nbsp; {data.totalCount} users
              </Typography>
              <Button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </Box>
          )}
        </>
      )}

      {/* Add User dialog */}
      <CreateUserDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      {/* Edit User dialog */}
      {editUser && <EditUserDialog user={editUser} onClose={() => setEditUser(null)} />}

      {/* Deactivate confirmation dialog */}
      <Dialog open={!!deactivateTarget} onClose={() => setDeactivateTarget(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Deactivate User</DialogTitle>
        <DialogContent>
          <ApiErrorAlert error={deactivateMutation.error} />
          <Typography>
            Deactivate <strong>{deactivateTarget?.fullName}</strong>? They will no longer be able to log in.
            This action can be reversed by editing their account.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeactivateTarget(null)}>Cancel</Button>
          <Button
            color="error"
            variant="contained"
            disabled={deactivateMutation.isPending}
            onClick={() => deactivateTarget && deactivateMutation.mutate(deactivateTarget.id)}
          >
            Deactivate
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
