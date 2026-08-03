import { createBrowserRouter, Navigate, useNavigate, useLoaderData } from 'react-router-dom';
import { buildingApi } from '@shared/api/endpoints';
import { useEffect } from 'react';
import { AppLayout } from '@shared/components/AppLayout';
import { BuildingOverviewPage } from '@pages/building-overview/BuildingOverviewPage';
import { RoomDetailsPage } from '@pages/room-details/RoomDetailsPage';
import { EnvironmentHistoryPage } from '@pages/room-details/EnvironmentHistoryPage';
import { DevicesPage } from '@pages/devices/DevicesPage';
import { DeviceDetailPage } from '@pages/devices/DeviceDetailPage';
import { DeviceRegistrationPage } from '@pages/device-registration/DeviceRegistrationPage';
import { NotFoundPage } from '@pages/not-found/NotFoundPage';
import { MockControlsPage } from '@pages/mock-controls/MockControlsPage';
import { NeedsPage } from '@pages/needs/NeedsPage';
import { NeedDetailPage } from '@pages/needs/NeedDetailPage';
import { EngineeringSystemsPage } from '@pages/engineering-systems/EngineeringSystemsPage';
import { EngineeringSystemDetailPage } from '@pages/engineering-systems/EngineeringSystemDetailPage';
import { CommandPlansPage } from '@pages/command-plans/CommandPlansPage';
import { CommandPlanDetailPage } from '@pages/command-plans/CommandPlanDetailPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <BuildingRedirect /> },
      { path: 'buildings/:buildingId', element: <BuildingOverviewPage /> },
      { path: 'rooms/:roomId', element: <RoomDetailsPage /> },
      { path: 'rooms/:roomId/history', element: <EnvironmentHistoryPage /> },
      { path: 'rooms/:roomId/devices', element: <DevicesPage /> },
      { path: 'devices', element: <DevicesPage /> },
      { path: 'devices/:deviceId', element: <DeviceDetailPage /> },
      { path: 'devices/register', element: <DeviceRegistrationPage /> },
      { path: 'dev/mock-controls', element: <MockControlsPage /> },
      { path: 'needs', element: <NeedsPage /> },
      { path: 'needs/:needId', element: <NeedDetailPage /> },
      { path: 'engineering-systems', element: <EngineeringSystemsPage /> },
      { path: 'engineering-systems/:id', element: <EngineeringSystemDetailPage /> },
      { path: 'command-plans', element: <CommandPlansPage /> },
      { path: 'command-plans/:id', element: <CommandPlanDetailPage /> },
      { path: '*', element: <NotFoundPage /> },
      { path: 'buildings', element: <BuildingListPage /> },
    ],
  },
]);