import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@/components/ui';
import { useBlocker } from 'react-router-dom';

interface UnsavedChangesDialogProps {
  when: boolean;
}

export function UnsavedChangesDialog({ when }: UnsavedChangesDialogProps) {
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      when && currentLocation.pathname !== nextLocation.pathname,
  );

  const isBlocked = blocker.state === 'blocked';

  return (
    <Dialog open={isBlocked}>
      <DialogTitle>Unsaved changes</DialogTitle>
      <DialogContent>
        <DialogContentText>
          You have unsaved changes. Are you sure you want to leave? Your changes will be lost.
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => blocker.reset?.()}>Stay</Button>
        <Button onClick={() => blocker.proceed?.()} variant="contained" color="warning">
          Leave
        </Button>
      </DialogActions>
    </Dialog>
  );
}
