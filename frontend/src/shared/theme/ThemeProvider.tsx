import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '@app/queryClient';
import { theme } from './theme';
import { useAppearance } from './useAppearance';
import type { ReactNode } from 'react';

interface ThemeProviderProps {
  children: ReactNode;
}

function InnerThemeProvider({ children }: ThemeProviderProps) {
  const { resolved } = useAppearance();

  return (
    <MantineProvider theme={theme} forceColorScheme={resolved}>
      <Notifications position="top-right" />
      {children}
    </MantineProvider>
  );
}

export function ThemeProvider({ children }: ThemeProviderProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <InnerThemeProvider>{children}</InnerThemeProvider>
    </QueryClientProvider>
  );
}