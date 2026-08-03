import { useNavigate } from 'react-router-dom';
import { useBuildings } from '@features/building-navigation/hooks';
import { useEffect } from 'react';

export function BuildingListPage() {
  const navigate = useNavigate();
  const { data: buildings, isLoading } = useBuildings();

  useEffect(() => {
    if (!isLoading && buildings && buildings.length > 0) {
      navigate(`/buildings/${buildings[0].id}`, { replace: true });
    }
  }, [buildings, isLoading, navigate]);

  if (isLoading) return <div>Loading buildings...</div>;
  if (!buildings || buildings.length === 0) return <div>No buildings found. Create one to get started.</div>;
  return <div>Redirecting...</div>;
}