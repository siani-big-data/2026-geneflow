from abc import ABC, abstractmethod
from datetime import datetime


class StorageProvider(ABC):
    """Interface para providers de almacenamiento."""

    @abstractmethod
    async def append_events_batch(
            self, category: str, date: datetime, event_lines: list[str]
    ) -> None:
        """Añadir batch de eventos a un archivo."""
        pass

    @abstractmethod
    async def read_events(self, category: str, date: datetime) -> list[str]:
        """Leer eventos de un día."""
        pass

    @abstractmethod
    async def read_events_range(
            self, category: str, start_date: datetime, end_date: datetime
    ) -> list[str]:
        """Leer eventos de un rango de fechas."""
        pass

    @abstractmethod
    async def list_categories(self) -> list[str]:
        """Listar categorías con datos."""
        pass

    @abstractmethod
    async def list_dates(self, category: str) -> list[datetime]:
        """Listar fechas disponibles para una categoría."""
        pass

    @abstractmethod
    async def get_stats(self, category: str) -> dict:
        """Obtener estadísticas de una categoría."""
        pass

    @abstractmethod
    async def health_check(self) -> bool:
        """Verificar salud del storage."""
        pass

    @abstractmethod
    async def close(self) -> None:
        """Cerrar conexiones/recursos."""
        pass

    def _get_file_path(self, category: str, date: datetime) -> str:
        """Helper: path del archivo JSONL."""
        date_str = date.strftime("%Y-%m-%d")
        return f"events/{category}/{date_str}.jsonl"