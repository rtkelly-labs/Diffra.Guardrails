from dataclasses import dataclass
from typing import List

@dataclass(frozen=True)
class TeamDto:
    id: int
    members: List[str]

def test_contract_reads_members():
    # Arrange
    team = TeamDto(1, ["Alice", "Bob"])

    # Act
    count = len(team.members)

    # Assert
    assert count == 2, "two members present in team"
