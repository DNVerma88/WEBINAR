import { Alert, Box, Button, Chip, LinearProgress, List, Stack, Tab, Tabs, TextField, Typography } from '@/components/ui';
import { CloudUploadIcon, DeleteIcon } from '@/components/ui';
import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { useMutation } from '@tanstack/react-query';
import { screeningApi } from '../../talentApi';
import { useOneDrivePicker } from '../../hooks/useOneDrivePicker';
import type { StorageFileRef } from '../../types';
import { Paper, ListItem, ListItemText, ListItemSecondaryAction, IconButton } from '@mui/material';
import { StorageBrowserDialog } from './StorageBrowserDialog';
import { useToast } from '../../../../shared/hooks/useToast';

interface Props {
  jobId: string;
  onFilesAdded: () => void;
}

export function ResumeSourcePanel({ jobId, onFilesAdded }: Props) {
  const toast = useToast();
  const [tab, setTab] = useState(0);
  const [localFiles, setLocalFiles] = useState<File[]>([]);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [tenantUrl, setTenantUrl] = useState('');
  const [s3BrowserOpen, setS3BrowserOpen] = useState(false);
  const [azureBrowserOpen, setAzureBrowserOpen] = useState(false);

  // Validate that the tenant URL is an absolute https:// URL
  const isTenantUrlValid =
    tenantUrl.trim().length > 0 &&
    (() => {
      try {
        const u = new URL(tenantUrl.trim());
        return u.protocol === 'https:';
      } catch {
        return false;
      }
    })();

  const uploadMutation = useMutation({
    mutationFn: (files: File[]) =>
      screeningApi.uploadFiles(jobId, files, setUploadProgress),
    onSuccess: () => {
      setLocalFiles([]);
      setUploadProgress(0);
      onFilesAdded();
      toast.success('Resumes uploaded.');
    },
    onError: () => setUploadError('Upload failed. Please try again.'),
  });

  const addStorageMutation = useMutation({
    mutationFn: (refs: StorageFileRef[]) => screeningApi.addFromStorage(jobId, refs),
    onSuccess: () => {
      clearSelection();
      onFilesAdded();
      toast.success('Resumes added from storage.');
    },
    onError: () => toast.error('Failed to add resumes from storage.'),
  });

  const onDrop = useCallback(
    (accepted: File[]) => setLocalFiles((prev) => [...prev, ...accepted]),
    [],
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'application/pdf': ['.pdf'], 'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'] },
    multiple: true,
  });

  const { openPicker, selectedFiles, clearSelection } = useOneDrivePicker({
    tenantUrl,
    clientId: '',
    multiSelect: true,
  });

  const removeLocalFile = (index: number) =>
    setLocalFiles((prev) => prev.filter((_, i) => i !== index));

  return (
    <Box>
      <Tabs value={tab} onChange={(_, v: number) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Upload Files" />
        <Tab label="OneDrive / SharePoint" />
        <Tab label="Amazon S3" />
        <Tab label="Azure Blob" />
      </Tabs>

      {tab === 0 && (
        <Box>
          <Paper
            {...getRootProps()}
            variant="outlined"
            sx={{
              p: 4,
              textAlign: 'center',
              cursor: 'pointer',
              borderStyle: 'dashed',
              bgcolor: isDragActive ? 'action.hover' : 'background.paper',
              mb: 2,
            }}
          >
            <input {...getInputProps()} />
            <CloudUploadIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }} />
            <Typography>
              {isDragActive ? 'Drop files here…' : 'Drag and drop PDF/DOCX files, or click to select'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Supports .pdf and .docx
            </Typography>
          </Paper>

          {localFiles.length > 0 && (
            <>
              <List dense>
                {localFiles.map((f, i) => (
                  <ListItem key={i} divider>
                    <ListItemText
                      primary={f.name}
                      secondary={`${(f.size / 1024).toFixed(1)} KB`}
                    />
                    <ListItemSecondaryAction>
                      <IconButton size="small" onClick={() => removeLocalFile(i)}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
              </List>

              {uploadMutation.isPending && (
                <Box mt={1}>
                  <LinearProgress variant="determinate" value={uploadProgress} />
                  <Typography variant="caption">{uploadProgress}%</Typography>
                </Box>
              )}

              {uploadError && (
                <Alert severity="error" sx={{ mt: 1 }}>
                  {uploadError}
                </Alert>
              )}

              <Button
                variant="contained"
                sx={{ mt: 2 }}
                onClick={() => uploadMutation.mutate(localFiles)}
                disabled={uploadMutation.isPending}
              >
                Upload {localFiles.length} file{localFiles.length !== 1 ? 's' : ''}
              </Button>
            </>
          )}
        </Box>
      )}

      {tab === 1 && (
        <Box>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Browse and select resumes from your OneDrive or SharePoint.
          </Typography>
          <TextField
            label="SharePoint / OneDrive URL"
            placeholder="https://yourtenant.sharepoint.com"
            size="small"
            fullWidth
            value={tenantUrl}
            onChange={(e) => setTenantUrl(e.target.value)}
            sx={{ mb: 2 }}
            error={tenantUrl.trim().length > 0 && !isTenantUrlValid}
            helperText={
              tenantUrl.trim().length > 0 && !isTenantUrlValid
                ? 'Enter a valid URL starting with https://'
                : 'Enter your SharePoint site URL to enable the file picker.'
            }
          />
          <Button
            variant="outlined"
            onClick={() => void openPicker()}
            disabled={!isTenantUrlValid}
          >
            Browse OneDrive / SharePoint
          </Button>
          {selectedFiles.length > 0 && (
            <Box mt={2}>
              <Stack direction="row" flexWrap="wrap" gap={1} mb={2}>
                {selectedFiles.map((f) => (
                  <Chip key={f.fileId} label={f.fileName} />
                ))}
              </Stack>
              <Button
                variant="contained"
                onClick={() => addStorageMutation.mutate(selectedFiles)}
                disabled={addStorageMutation.isPending}
              >
                Add {selectedFiles.length} file{selectedFiles.length !== 1 ? 's' : ''}
              </Button>
            </Box>
          )}
        </Box>
      )}

      {tab === 2 && (
        <Box>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Browse and select resume files from Amazon S3.
          </Typography>
          <Button variant="outlined" onClick={() => setS3BrowserOpen(true)}>
            Browse S3
          </Button>
          <StorageBrowserDialog
            open={s3BrowserOpen}
            onClose={() => setS3BrowserOpen(false)}
            provider="S3"
            onFilesSelected={(refs) => addStorageMutation.mutate(refs)}
          />
        </Box>
      )}

      {tab === 3 && (
        <Box>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Browse and select resume files from Azure Blob Storage.
          </Typography>
          <Button variant="outlined" onClick={() => setAzureBrowserOpen(true)}>
            Browse Azure Blob
          </Button>
          <StorageBrowserDialog
            open={azureBrowserOpen}
            onClose={() => setAzureBrowserOpen(false)}
            provider="AzureBlob"
            onFilesSelected={(refs) => addStorageMutation.mutate(refs)}
          />
        </Box>
      )}
    </Box>
  );
}
