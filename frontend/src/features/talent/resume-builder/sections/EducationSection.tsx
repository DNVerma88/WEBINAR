import { Box, Button, Card, CardContent, Grid, IconButton, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { Controller, useFieldArray } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function EducationSection({ control }: Props) {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'education',
    keyName: 'rhfId',
  });

  return (
    <Box>
      {fields.map((field, i) => (
        <Card key={field.rhfId} sx={{ mb: 2 }} variant="outlined">
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="subtitle2">Education #{i + 1}</Typography>
              <IconButton size="small" color="error" onClick={() => remove(i)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`education.${i}.degree`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Degree / Qualification" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`education.${i}.institution`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Institution" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`education.${i}.startYear`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Start Year" placeholder="e.g. 2016" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`education.${i}.endYear`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="End Year" placeholder="e.g. 2020" fullWidth />
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
            degree: '',
            institution: '',
            startYear: '',
            endYear: '',
          })
        }
        variant="outlined"
      >
        Add Education
      </Button>
    </Box>
  );
}
