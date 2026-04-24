import { Box, Button, Tab, Tabs, TextField, Typography } from '@/components/ui';
import { CircularProgress } from '@mui/material';
import { MenuItem, Select } from '@mui/material';
import { useState, useRef } from 'react';
import type { StorageFileRef } from '../../types';
import { StorageBrowserDialog } from './StorageBrowserDialog';
import { useOneDrivePicker } from '../../hooks/useOneDrivePicker';
import { screeningApi } from '../../talentApi';

interface Props {
  onJdReady: (jdText?: string, jdFileRef?: StorageFileRef) => void;
}

export function JdInputPanel({ onJdReady }: Props) {
  const [tab, setTab] = useState(0);
  const [jdText, setJdText] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [extracting, setExtracting] = useState(false);
  const [extractError, setExtractError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [cloudProvider, setCloudProvider] = useState<'S3' | 'AzureBlob' | 'OneDrive'>('S3');
  const [browserOpen, setBrowserOpen] = useState(false);
  const [cloudFile, setCloudFile] = useState<StorageFileRef | null>(null);
  const [tenantUrl, setTenantUrl] = useState('');

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

  const { openPicker, selectedFiles: odFiles } = useOneDrivePicker({
    tenantUrl,
    clientId: '',
    multiSelect: false,
  });

  // When OneDrive picker returns a file, propagate it up
  const prevOdFilesRef = useRef<typeof odFiles>([]);
  if (odFiles.length > 0 && odFiles !== prevOdFilesRef.current) {
    prevOdFilesRef.current = odFiles;
    const ref = odFiles[0];
    if (ref) {
      setCloudFile(ref);
      onJdReady(undefined, ref);
    }
  }

  const handleTextChange = (text: string) => {
    setJdText(text);
    onJdReady(text || undefined);
  };

  const handleFileSelect = async (file: File) => {
    setSelectedFile(file);
    setExtractError(null);
    setExtracting(true);
    try {
      const text = await screeningApi.extractJdText(file);
      onJdReady(text || undefined);
    } catch {
      setExtractError('Could not extract text from the file. Please try a different file or paste the text manually.');
      onJdReady(undefined);
    } finally {
      setExtracting(false);
    }
  };

  return (
    <Box>
      <Tabs value={tab} onChange={(_, v: number) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Paste Text" />
        <Tab label="Upload File" />
        <Tab label="Cloud Storage" />
      </Tabs>

      {tab === 0 && (
        <TextField
          label="Job Description"
          multiline
          rows={8}
          fullWidth
          value={jdText}
          onChange={(e) => handleTextChange(e.target.value)}
          placeholder="Paste the job description here..."
        />
      )}

      {tab === 1 && (
        <Box>
          <input
            type="file"
            accept=".pdf,.docx"
            ref={fileInputRef}
            style={{ display: 'none' }}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) void handleFileSelect(file);
            }}
          />
          <Button
            variant="outlined"
            onClick={() => fileInputRef.current?.click()}
            disabled={extracting}
          >
            Choose File (.pdf or .docx)
          </Button>
          {extracting && (
            <Box display="flex" alignItems="center" gap={1} mt={1}>
              <CircularProgress size={16} />
              <Typography variant="body2" color="text.secondary">
                Extracting text from {selectedFile?.name}…
              </Typography>
            </Box>
          )}
          {!extracting && selectedFile && !extractError && (
            <Typography variant="body2" sx={{ mt: 1 }} color="success.main">
              Text extracted from {selectedFile.name}
            </Typography>
          )}
          {extractError && (
            <Typography variant="body2" sx={{ mt: 1 }} color="error">
              {extractError}
            </Typography>
          )}
        </Box>
      )}

      {tab === 2 && (
        <Box>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Select a JD file from your connected cloud storage.
          </Typography>
          <Box display="flex" alignItems="center" gap={1} mb={2}>
            <Select
              size="small"
              value={cloudProvider}
              onChange={(e) => {
                setCloudProvider(e.target.value as 'S3' | 'AzureBlob' | 'OneDrive');
                setCloudFile(null);
              }}
            >
              <MenuItem value="S3">Amazon S3</MenuItem>
              <MenuItem value="AzureBlob">Azure Blob Storage</MenuItem>
              <MenuItem value="OneDrive">OneDrive / SharePoint</MenuItem>
            </Select>
          </Box>

          {cloudProvider === 'OneDrive' && (
            <Box mb={2}>
              <TextField
                label="SharePoint / OneDrive URL"
                placeholder="https://yourtenant.sharepoint.com"
                size="small"
                fullWidth
                value={tenantUrl}
                onChange={(e) => setTenantUrl(e.target.value)}
                sx={{ mb: 1 }}
                error={tenantUrl.trim().length > 0 && !isTenantUrlValid}
                helperText={
                  tenantUrl.trim().length > 0 && !isTenantUrlValid
                    ? 'Enter a valid URL starting with https://'
                    : 'Enter your SharePoint site URL, then click Browse.'
                }
              />
              <Button
                variant="outlined"
                onClick={() => void openPicker()}
                disabled={!isTenantUrlValid}
              >
                Browse OneDrive / SharePoint
              </Button>
            </Box>
          )}

          {(cloudProvider === 'S3' || cloudProvider === 'AzureBlob') && (
            <Button variant="outlined" sx={{ mb: 2 }} onClick={() => setBrowserOpen(true)}>
              Browse {cloudProvider === 'S3' ? 'Amazon S3' : 'Azure Blob Storage'}
            </Button>
          )}

          {cloudFile && (
            <Typography variant="body2" color="text.secondary">
              Selected: {cloudFile.fileName}
            </Typography>
          )}

          {(cloudProvider === 'S3' || cloudProvider === 'AzureBlob') && (
            <StorageBrowserDialog
              open={browserOpen}
              onClose={() => setBrowserOpen(false)}
              provider={cloudProvider}
              multiSelect={false}
              onFilesSelected={(refs) => {
                const ref = refs[0];
                if (ref) {
                  setCloudFile(ref);
                  onJdReady(undefined, ref);
                }
              }}
            />
          )}
        </Box>
      )}
    </Box>
  );
}
