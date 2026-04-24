import { Box, Button, Card, CardContent, Grid, IconButton, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { Controller, useFieldArray } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function CertificationsSection({ control }: Props) {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'certifications',
    keyName: 'rhfId',
  });

  return (
    <Box>
      {fields.map((field, i) => (
        <Card key={field.rhfId} sx={{ mb: 2 }} variant="outlined">
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="subtitle2">Certification #{i + 1}</Typography>
              <IconButton size="small" color="error" onClick={() => remove(i)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`certifications.${i}.name`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Certification Name" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`certifications.${i}.issuer`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Issuing Organisation" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`certifications.${i}.date`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Date" placeholder="e.g. Mar 2023" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`certifications.${i}.url`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Credential URL" fullWidth />
                  )}
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      ))}
      <Button
        startIcon={<AddIcon />}
        onClick={() =>
          append({
            id: crypto.randomUUID(),
            name: '',
            issuer: '',
            date: '',
            url: '',
          })
        }
        variant="outlined"
      >
        Add Certification
      </Button>
    </Box>
  );
}
