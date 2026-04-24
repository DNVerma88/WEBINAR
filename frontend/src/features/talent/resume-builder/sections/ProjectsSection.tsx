import { Box, Button, Card, CardContent, Grid, IconButton, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { Controller, useFieldArray } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function ProjectsSection({ control }: Props) {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'projects',
    keyName: 'rhfId',
  });

  return (
    <Box>
      {fields.map((field, i) => (
        <Card key={field.rhfId} sx={{ mb: 2 }} variant="outlined">
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="subtitle2">Project #{i + 1}</Typography>
              <IconButton size="small" color="error" onClick={() => remove(i)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`projects.${i}.company`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Company / Organisation" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`projects.${i}.name`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Project Name" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`projects.${i}.url`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Project URL" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Controller
                  name={`projects.${i}.technologies`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Technologies Used" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12 }}>
                <Controller
                  name={`projects.${i}.description`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField
                      {...f}
                      label="Description"
                      multiline
                      rows={3}
                      fullWidth
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
            name: '',
            description: '',
            url: '',
            technologies: '',
            company: '',
          })
        }
        variant="outlined"
      >
        Add Project
      </Button>
    </Box>
  );
}
