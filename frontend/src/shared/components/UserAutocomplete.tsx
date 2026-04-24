import { useState } from 'react';
import { Autocomplete, Box, TextField } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../api/users';
import type { UserDto } from '../types';

interface Props {
  value: UserDto | null;
  onChange: (user: UserDto | null) => void;
  label?: string;
  required?: boolean;
  size?: 'small' | 'medium';
  /** User IDs to exclude from the options list (e.g. already-added members). */
  filterIds?: string[];
  helperText?: string;
  fullWidth?: boolean;
}

/**
 * Searchable user autocomplete that queries GET /users with the typed text.
 * Accepts a UserDto as the controlled value so callers can read `.id` for submission.
 */
export function UserAutocomplete({
  value,
  onChange,
  label = 'User',
  required = false,
  size = 'medium',
  filterIds = [],
  helperText,
  fullWidth = false,
}: Props) {
  const [search, setSearch] = useState('');

  const { data } = useQuery({
    queryKey: ['users-autocomplete', search],
    queryFn: () => usersApi.getUsers({ search: search || undefined, isActive: true, pageSize: 50 }),
  });

  const baseOptions = data?.data ?? [];
  const filtered = filterIds.length
    ? baseOptions.filter((u) => !filterIds.includes(u.id))
    : baseOptions;

  // When a value is pre-set (e.g. editing an existing record) and the current
  // search results don't include it, prepend it so the input displays the name.
  const options =
    value && !filtered.some((u) => u.id === value.id) ? [value, ...filtered] : filtered;

  return (
    <Autocomplete
      value={value}
      onChange={(_, newValue) => onChange(newValue)}
      onInputChange={(_, newValue) => setSearch(newValue)}
      filterOptions={(x) => x}
      options={options}
      getOptionLabel={(u) => u.fullName}
      isOptionEqualToValue={(o, v) => o.id === v.id}
      fullWidth={fullWidth}
      size={size}
      renderOption={(props, u) => (
        <li {...props} key={u.id}>
          <Box>
            <Box component="span" sx={{ display: 'block', fontSize: '0.875rem' }}>
              {u.fullName}
            </Box>
            {(u.designation || u.department) && (
              <Box
                component="span"
                sx={{ display: 'block', fontSize: '0.75rem', color: 'text.secondary' }}
              >
                {[u.designation, u.department].filter(Boolean).join(' · ')}
              </Box>
            )}
          </Box>
        </li>
      )}
      renderInput={(params) => (
        <TextField {...params} label={label} required={required} helperText={helperText} />
      )}
    />
  );
}
