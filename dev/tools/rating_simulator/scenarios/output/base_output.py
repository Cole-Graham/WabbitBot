"""Common utilities for generating output files."""

from typing import List, Dict, Any


def get_max_width(
    header: str, rows: List[Dict[str, Any]], key: str, format_str: str = "{:.1f}"
) -> int:
    """Calculate the maximum width needed for a column based on header and data.

    Args:
        header: The column header text
        rows: List of dictionaries containing the data
        key: The key to look up in each row
        format_str: Format string for numeric values (default: "{:.1f}")

    Returns:
        The maximum width needed for the column
    """
    # Start with header length
    max_width = len(header)

    # Check each row's value
    for row in rows:
        value = row.get(key, "")
        if isinstance(value, (int, float)):
            # Format numeric values
            value_str = format_str.format(value)
        else:
            value_str = str(value)
        max_width = max(max_width, len(value_str))

    return max_width
