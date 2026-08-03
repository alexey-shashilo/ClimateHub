import { useEffect, useState } from 'react';
import { RouterProvider } from 'react-router-dom';
import { LoadingOverlay, Box } from '@mantine/core';
import { ThemeProvider } from '@shared/theme/ThemeProvider';
import { router } from './router';

export function App() {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    async function init() {
      if (import.meta.env.VITE_ENABLE_MOCKS === 'true') {
        const { worker } = await import('@mocks/browser');
        await worker.start({
          onUnhandledRequest: 'bypass',
        });
      }
      setReady(true);
    }
    init();
  }, []);

  if (!ready) {
    return (
      <ThemeProvider>
        <Box h="100vh">
          <LoadingOverlay visible={true} />
        </Box>
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider>
      <RouterProvider router={router} />
    </ThemeProvider>
  );
}