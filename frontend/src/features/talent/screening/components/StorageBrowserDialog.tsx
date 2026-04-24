import { Box, Button, Chip, Stack, Typography } from '@/components/ui';
import {
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import FolderIcon from '@mui/icons-material/Folder';
import InsertDriveFileIcon from '@mui/icons-material/InsertDriveFile';
import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { storageApi } from '../../storageApi';
import type { StorageFileItem, StorageFileRef } from '../../types';

interface Props {
  open: boolean;
  onClose: () => void;
  provider: 'S3' | 'AzureBlob';
  onFilesSelected: (files: StorageFileRef[]) => void;
  /** Allow selecting multiple files (default: true) */
  multiSelect?: boolean;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1_048_576) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1_048_576).toFixed(1)} MB`;
}

export function StorageBrowserDialog({
  open,
  onClose,
  provider,
  onFilesSelected,
  multiSelect = true,
}: Props) {
  const [path, setPath] = useState('');
  const [selected, setSelected] = useState<StorageFileItem[]>([]);

  // Reset when dialog opens/closes
  useEffect(() => {
    if (open) { setPath(''); setSelected([]); }
  }, [open]);

  const { data: items = [], isLoading, isError } = useQuery({
    queryKey: ['storage', 'list', provider, path],
    queryFn: () => storageApi.listFiles(provider, path),
    enabled: open,
  });

  // Separate folders (no extension) from files (.pdf/.docx)
  const folders = items.filter(
    (i) => !i.fileName.includes('.') || i.mimeType === 'application/x-directory',
  );
  const files = items.filter(
    (i) => i.fileName.endsWith('.pdf') || i.fileName.endsWith('.docx'),
  );

  const toggleSelect = (item: StorageFileItem) => {
    if (!multiSelect) {
      setSelected([item]);
      return;
    }
    setSelected((prev) =>
      prev.some((s) => s.fileId === item.fileId)
        ? prev.filter((s) => s.fileId !== item.fileId)
        : [...prev, item],
    );
  };

  const isSelected = (item: StorageFileItem) =>
    selected.some((s) => s.fileId === item.fileId);

  const navigateInto = (folder: StorageFileItem) => {
    setPath((prev) => (prev ? `${prev}/${folder.fileName}` : folder.fileName));
    setSelected([]);
  };

  const navigateUp = () => {
    const parts = path.split('/');
    parts.pop();
    setPath(parts.join('/'));
    setSelected([]);
  };

  const handleConfirm = () => {
    const refs: StorageFileRef[] = selected.map((item) => ({
      providerType: provider,
      fileId: item.fileId,
      fileName: item.fileName,
      fileSizeBytes: item.sizeBytes,
    }));
    onFilesSelected(refs);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        <Stack direction="row" alignItems="center" spacing={1}>
          {path && (
            <IconButton size="small" onClick={navigateUp}>
              <ArrowBackIcon fontSize="small" />
            </IconButton>
          )}
          <Box>
            <Typography variant="subtitle1" fontWeight={600}>
              {provider === 'S3' ? 'Amazon S3' : 'Azure Blob Storage'}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              /{path || '(root)'}
            </Typography>
          </Box>
        </Stack>
      </DialogTitle>

      <DialogContent dividers sx={{ minHeight: 300 }}>
        {isLoading && (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress size={32} />
          </Box>
        )}

        {isError && (
          <Typography color="error" variant="body2" py={2}>
            Failed to load files. Check that the {provider === 'S3' ? 'S3' : 'Azure Blob'} provider
            is configured in application settings.
          </Typography>
        )}

        {!isLoading && !isError && items.length === 0 && (
          <Typography color="text.secondary" variant="body2" py={2}>
            This folder is empty.
          </Typography>
        )}

        {!isLoading && !isError && (
          <List dense>
            {folders.map((folder) => (
              <ListItemButton
                key={folder.fileId}
                onClick={() => navigateInto(folder)}
              >
                <ListItemIcon>
                  <FolderIcon color="warning" />
                </ListItemIcon>
                <ListItemText primary={folder.fileName} />
              </ListItemButton>
            ))}
            {files.map((file) => (
              <ListItemButton
                key={file.fileId}
                onClick={() => toggleSelect(file)}
                selected={isSelected(file)}
              >
                {multiSelect && (
                  <ListItemIcon>
                    <Checkbox
                      edge="start"
                      checked={isSelected(file)}
                      tabIndex={-1}
                      disableRipple
                      size="small"
                    />
                  </ListItemIcon>
                )}
                <ListItemIcon>
                  <InsertDriveFileIcon color="action" />
                </ListItemIcon>
                <ListItemText
                  primary={file.fileName}
                  secondary={formatSize(file.sizeBytes)}
                />
              </ListItemButton>
            ))}
          </List>
        )}
      </DialogContent>

      <DialogActions sx={{ justifyContent: 'space-between', px: 2 }}>
        <Box>
          {selected.length > 0 && (
            <Stack direction="row" spacing={0.5} flexWrap="wrap">
              {selected.map((s) => (
                <Chip
                  key={s.fileId}
                  label={s.fileName}
                  size="small"
                  onDelete={() => toggleSelect(s)}
                />
              ))}
            </Stack>
          )}
        </Box>
        <Stack direction="row" spacing={1}>
          <Button onClick={onClose}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleConfirm}
            disabled={selected.length === 0}
          >
            Select {selected.length > 0 ? `(${selected.length})` : ''}
          </Button>
        </Stack>
      </DialogActions>
    </Dialog>
  );
}
