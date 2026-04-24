import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  List,
  ListItem,
  ListItemText,
  Typography,
} from '@mui/material';
import { CircularProgress } from '@mui/material';

interface ResendDialogProps {
  open: boolean;
  userNames: string[];
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
}

export function ResendDialog({ open, userNames, onConfirm, onCancel, loading = false }: ResendDialogProps) {
  return (
    <Dialog open={open} onClose={loading ? undefined : onCancel} maxWidth="xs" fullWidth>
      <DialogTitle>Resend Invitations</DialogTitle>
      <DialogContent>
        <Typography variant="body2" sx={{ mb: 1 }}>
          Resend the survey link to {userNames.length} user{userNames.length !== 1 ? 's' : ''}?
        </Typography>
        <List dense sx={{ maxHeight: 200, overflow: 'auto' }}>
          {userNames.map((name, i) => (
            <ListItem key={i} disableGutters>
              <ListItemText primary={name} />
            </ListItem>
          ))}
        </List>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={loading}>Cancel</Button>
        <Button
          variant="contained"
          onClick={onConfirm}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} /> : undefined}
        >
          {loading ? 'Sending…' : 'Resend'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
