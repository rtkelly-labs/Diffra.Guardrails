from dataclasses import dataclass
from typing import Tuple


@dataclass(frozen=True)
class Order:
    items: Tuple[int, ...]

    @property
    def item_count(self) -> int:
        return len(self.items)


def test_clean_functional_pipeline_executes_purely():
    # Arrange
    initial = Order(items=(10, 20))
    next_item = 30

    # Act
    updated = Order(items=(*initial.items, next_item))

    # Assert
    assert len([x for x in updated.items if x > 15]) == 2, "two items exceed fifteen in the pure pipeline"
