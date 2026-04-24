import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@/components/ui';
import { FormControl, InputLabel, MenuItem, Select } from '@mui/material';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { assessmentGroupApi, workRoleApi } from '../api/assessmentApi';
import { LoadingOverlay } from '../../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { UserAutocomplete } from '../../../shared/components/UserAutocomplete';
import type { AssessmentGroupDto } from '../types';
import type { UserDto } from '../../../shared/types';
import { useToast } from '../../../shared/hooks/useToast';

type DialogMode = 'create' | 'edit' | 'members' | null;

const EMPTY_FORM = { groupName: '', groupCode: '', description: '' };

export function GroupsTab() {
  const qc = useQueryClient();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState<DialogMode>(null);
  const [editing, setEditing] = useState<AssessmentGroupDto | null>(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [selectedPrimaryLead, setSelectedPrimaryLead] = useState<UserDto | null>(null);
  const [selectedCoLead, setSelectedCoLead] = useState<UserDto | null>(null);
  const [selectedMember, setSelectedMember] = useState<UserDto | null>(null);
  const [selectedWorkRoleId, setSelectedWorkRoleId] = useState('');
  const [membersGroupId, setMembersGroupId] = useState('');
  const [mutError, setMutError] = useState<Error | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: ['assessment', 'groups', page],
    queryFn: () => assessmentGroupApi.getGroups({ pageNumber: page, pageSize: 20 }),
  });

  const { data: members, isLoading: loadingMembers } = useQuery({
    queryKey: ['assessment', 'group-members', membersGroupId],
    queryFn: () => assessmentGroupApi.getMembers(membersGroupId),
    enabled: !!membersGroupId && dialog === 'members',
  });

  const { data: workRoles } = useQuery({
    queryKey: ['assessment', 'work-roles'],
    queryFn: () => workRoleApi.getWorkRoles(true),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['assessment', 'groups'] });

  const createMut = useMutation({
    mutationFn: () => assessmentGroupApi.createGroup({
      groupName: form.groupName, groupCode: form.groupCode,
      description: form.description || undefined,
      primaryLeadUserId: (selectedPrimaryLead?.id ?? '') as never,
      coLeadUserId: selectedCoLead?.id,
    } as never),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Group created.'); },
    onError: setMutError,
  });

  const updateMut = useMutation({
    mutationFn: () => assessmentGroupApi.updateGroup(editing!.id, {
      groupName: form.groupName, description: form.description || undefined,
      primaryLeadUserId: (selectedPrimaryLead?.id ?? '') as never,
      coLeadUserId: selectedCoLead?.id,
      isActive: editing!.isActive, recordVersion: editing!.recordVersion,
    } as never),
    onSuccess: () => { invalidate(); setDialog(null); setMutError(null); toast.success('Group updated.'); },
    onError: setMutError,
  });

  const deactivateMut = useMutation({
    mutationFn: (id: string) => assessmentGroupApi.deactivateGroup(id),
    onSuccess: () => { invalidate(); toast.success('Group deactivated.'); },
    onError: () => toast.error('Failed to deactivate group.'),
  });

  const addMemberMut = useMutation({
    mutationFn: () => assessmentGroupApi.addMember(membersGroupId, {
      userId: (selectedMember?.id ?? '') as never,
      workRoleId: selectedWorkRoleId || undefined,
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['assessment', 'group-members', membersGroupId] }); setSelectedMember(null); setSelectedWorkRoleId(''); toast.success('Member added.'); },
    onError: setMutError,
  });

  const removeMemberMut = useMutation({
    mutationFn: (userId: string) => assessmentGroupApi.removeMember(membersGroupId, userId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['assessment', 'group-members', membersGroupId] }); toast.success('Member removed.'); },
    onError: () => toast.error('Failed to remove member.'),
  });

  const openCreate = () => { setForm(EMPTY_FORM); setSelectedPrimaryLead(null); setSelectedCoLead(null); setEditing(null); setMutError(null); setDialog('create'); };
  const openEdit = (g: AssessmentGroupDto) => {
    setForm({ groupName: g.groupName, groupCode: g.groupCode, description: g.description ?? '' });
    setSelectedPrimaryLead({ id: g.primaryLeadUserId, fullName: g.primaryLeadName ?? '' } as UserDto);
    setSelectedCoLead(g.coLeadUserId ? { id: g.coLeadUserId, fullName: g.coLeadName ?? '' } as UserDto : null);
    setEditing(g); setMutError(null); setDialog('edit');
  };
  const openMembers = (g: AssessmentGroupDto) => { setMembersGroupId(g.id); setSelectedMember(null); setSelectedWorkRoleId(''); setMutError(null); setDialog('members'); };
  const handleSubmit = () => { dialog === 'create' ? createMut.mutate() : updateMut.mutate(); };
  const isBusy = createMut.isPending || updateMut.isPending;

  if (isLoading) return <LoadingOverlay />;
  if (error) return <ApiErrorAlert error={error} />;
  if (!data) return null;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h6">Assessment Groups ({data.totalCount})</Typography>
        <Button variant="contained" size="small" onClick={openCreate}>+ New Group</Button>
      </Stack>

      <Stack spacing={2}>
        {data.data.map((group) => (
          <Card key={group.id} variant="outlined">
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box>
                  <Typography fontWeight={700}>{group.groupName}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Code: {group.groupCode} · Primary Lead: {group.primaryLeadName}
                    {group.coLeadName && ` · Co-Lead: ${group.coLeadName}`}
                  </Typography>
                  {group.description && (
                    <Typography variant="body2" mt={0.5}>{group.description}</Typography>
                  )}
                  <Stack direction="row" spacing={1} mt={1}>
                    <Chip label={group.isActive ? 'Active' : 'Inactive'} size="small"
                      color={group.isActive ? 'success' : 'default'} />
                    <Chip label={`${group.activeEmployeeCount} employees`} size="small" variant="outlined" />
                  </Stack>
                </Box>
                <Stack direction="row" spacing={1}>
                  <Button size="small" variant="outlined" onClick={() => openEdit(group)}>Edit</Button>
                  <Button size="small" variant="outlined" onClick={() => openMembers(group)}>Members</Button>
                  {group.isActive && (
                    <Button size="small" color="error" variant="outlined"
                      onClick={() => deactivateMut.mutate(group.id)} disabled={deactivateMut.isPending}>
                      Deactivate
                    </Button>
                  )}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      {data.totalPages > 1 && (
        <Stack direction="row" spacing={1} mt={2} justifyContent="center">
          <Button disabled={page <= 1} onClick={() => setPage(p => p - 1)} size="small">Prev</Button>
          <Typography variant="body2" alignSelf="center">Page {data.pageNumber} of {data.totalPages}</Typography>
          <Button disabled={!data.hasNextPage} onClick={() => setPage(p => p + 1)} size="small">Next</Button>
        </Stack>
      )}

      {/* Create / Edit Dialog */}
      <Dialog open={dialog === 'create' || dialog === 'edit'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>{dialog === 'create' ? 'New Assessment Group' : 'Edit Group'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {mutError && <ApiErrorAlert error={mutError} />}
            <TextField label="Group Name" value={form.groupName} required fullWidth
              onChange={(e) => setForm(f => ({ ...f, groupName: e.target.value }))} />
            {dialog === 'create' && (
              <TextField label="Group Code" value={form.groupCode} required fullWidth
                onChange={(e) => setForm(f => ({ ...f, groupCode: e.target.value }))} />
            )}
            <TextField label="Description" value={form.description} fullWidth multiline rows={2}
              onChange={(e) => setForm(f => ({ ...f, description: e.target.value }))} />
            <UserAutocomplete
              value={selectedPrimaryLead}
              onChange={setSelectedPrimaryLead}
              label="Primary Lead"
              required
              fullWidth
            />
            <UserAutocomplete
              value={selectedCoLead}
              onChange={setSelectedCoLead}
              label="Co-Lead"
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Cancel</Button>
          <Button variant="contained" onClick={handleSubmit} disabled={isBusy || !form.groupName || !selectedPrimaryLead}>
            {isBusy ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Members Dialog */}
      <Dialog open={dialog === 'members'} onClose={() => setDialog(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Manage Members — {data.data.find(g => g.id === membersGroupId)?.groupName}</DialogTitle>
        <DialogContent>
          {loadingMembers ? <LoadingOverlay /> : (
            <Stack spacing={1} mt={1}>
              {mutError && <ApiErrorAlert error={mutError} />}
              <Stack direction="row" spacing={1} alignItems="flex-start">
                <UserAutocomplete
                  value={selectedMember}
                  onChange={setSelectedMember}
                  label="Add Member"
                  size="small"
                  fullWidth
                  filterIds={members?.map((m) => m.userId) ?? []}
                />                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel>Work Role</InputLabel>
                  <Select
                    value={selectedWorkRoleId}
                    label="Work Role"
                    onChange={e => setSelectedWorkRoleId(e.target.value)}
                  >
                    <MenuItem value=""><em>None</em></MenuItem>
                    {workRoles?.map(r => (
                      <MenuItem key={r.id} value={r.id}>{r.name}</MenuItem>
                    ))}
                  </Select>
                </FormControl>                <Button variant="contained" size="small" onClick={() => addMemberMut.mutate()}
                  disabled={addMemberMut.isPending || !selectedMember} sx={{ mt: 0.5 }}>Add</Button>
              </Stack>
              {members?.map((m) => (
                <Stack key={m.id} direction="row" justifyContent="space-between" alignItems="center"
                  sx={{ p: 1, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box>
                    <Typography variant="body2" fontWeight={600}>{m.fullName}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {m.designation}{m.workRoleName ? ` · ${m.workRoleName}` : ''} · {m.department}
                    </Typography>
                  </Box>
                  <Button size="small" color="error" onClick={() => removeMemberMut.mutate(m.userId)}>Remove</Button>
                </Stack>
              ))}
              {members?.length === 0 && <Typography color="text.secondary" variant="body2">No members assigned.</Typography>}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
