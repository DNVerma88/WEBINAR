import { Box, Button, Card, CardContent, Grid, IconButton, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { Controller, useFieldArray } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function WorkExperienceSection({ control }: Props) {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'workExperience',
    keyName: 'rhfId',
  });

  return (
    <Box>
      {fields.map((field, i) => (
        <Card key={field.rhfId} sx={{ mb: 2 }} variant="outlined">
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="subtitle2">Experience #{i + 1}</Typography>
              <IconButton size="small" color="error" onClick={() => remove(i)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`workExperience.${i}.jobTitle`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Job Title" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`workExperience.${i}.company`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Company" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`workExperience.${i}.startDate`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Start Date" placeholder="e.g. Jan 2020" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`workExperience.${i}.endDate`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="End Date" placeholder="e.g. Present" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12 }}>
                <Controller
                  name={`workExperience.${i}.description`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField
                      {...f}
                      label="Description"
                      multiline
                      rows={3}
                      fullWidth
                      placeholder="Describe your responsibilities and achievements..."
                    />
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
            jobTitle: '',
            company: '',
            startDate: '',
            endDate: '',
            description: '',
          })
        }
        variant="outlined"
      >
        Add Experience
      </Button>
    </Box>
  );
}
