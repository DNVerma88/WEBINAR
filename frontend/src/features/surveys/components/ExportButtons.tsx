import { useState } from 'react';
import { Button, CircularProgress, Stack } from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import { surveyAnalyticsApi } from '../api/surveyAnalyticsApi';

interface ExportButtonsProps {
  surveyId: string;
}

export function ExportButtons({ surveyId }: ExportButtonsProps) {
  const [csvLoading, setCsvLoading] = useState(false);
  const [pdfLoading, setPdfLoading] = useState(false);

  async function handleCsv() {
    setCsvLoading(true);
    try {
      await surveyAnalyticsApi.exportCsv(surveyId);
    } finally {
      setCsvLoading(false);
    }
  }

  async function handlePdf() {
    setPdfLoading(true);
    try {
      await surveyAnalyticsApi.exportPdf(surveyId);
    } finally {
      setPdfLoading(false);
    }
  }

  return (
    <Stack direction="row" spacing={1}>
      <Button
        variant="outlined"
        size="small"
        onClick={handleCsv}
        disabled={csvLoading}
        startIcon={csvLoading ? <CircularProgress size={14} /> : <DownloadIcon />}
      >
        Export CSV
      </Button>
      <Button
        variant="outlined"
        size="small"
        onClick={handlePdf}
        disabled={pdfLoading}
        startIcon={pdfLoading ? <CircularProgress size={14} /> : <PictureAsPdfIcon />}
      >
        Export PDF
      </Button>
    </Stack>
  );
}
