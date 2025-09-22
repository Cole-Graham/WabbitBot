import os
import sys
import argparse


def get_path_parts(full_path, workspace_root=None):
    """Get path components from workspace root to target directory"""
    parts = []
    current = full_path

    # Keep going up until we hit workspace root or filesystem root
    while current and os.path.basename(current):
        if workspace_root and os.path.abspath(current) == os.path.abspath(
            workspace_root
        ):
            break
        parts.append(os.path.basename(current))
        current = os.path.dirname(current)

    return list(reversed(parts))


def print_path_to_directory(path, workspace_root=None, file=sys.stdout, base_indent=""):
    """Print the path components leading to the target directory"""
    parts = get_path_parts(path, workspace_root)

    # Print path components from workspace root to target
    for i, part in enumerate(
        parts[:-1]
    ):  # Skip the last part as it will be printed by print_directory_structure
        print(f'{base_indent}{"  " * i}📁 {part}', file=file)
        if (
            i < len(parts) - 2
        ):  # Don't print the last connector as it will be printed by print_directory_structure
            print(f'{base_indent}{"  " * i}│', file=file)

    # Return the indentation level for the directory structure
    return base_indent + "  " * (len(parts) - 1) if parts else base_indent


def should_ignore_directory(name, include_dirs=None):
    """Check if directory should be ignored"""
    ignored_dirs = {"obj", "bin"}  # Set of directories to ignore
    if include_dirs is not None:
        return name.lower() not in {d.lower() for d in include_dirs}
    return name.lower() in ignored_dirs


def print_directory_structure(
    startpath, file=sys.stdout, indent="", is_root=True, include_dirs=None, depth=0
):
    # Only print the directory name if it's the root call
    if is_root:
        print(f"{indent}📁 {os.path.basename(startpath)}", file=file)
        indent += "  "

    try:
        # Get all entries in the directory, filtering out ignored directories
        # Only apply include_dirs filter at depth 1 (first level of target directory)
        should_filter = include_dirs is not None and depth == 0
        entries = [
            e
            for e in os.scandir(startpath)
            if not (
                e.is_dir()
                and should_ignore_directory(
                    e.name, include_dirs if should_filter else None
                )
            )
        ]
        entries = sorted(entries, key=lambda e: (not e.is_dir(), e.name))

        # Track if this is the last item for prettier printing
        for i, entry in enumerate(entries):
            is_last = i == len(entries) - 1

            if entry.is_dir():
                # Handle directories
                print(
                    f'{indent}{"└──" if is_last else "├──"} 📁 {entry.name}', file=file
                )
                # Recursively print contents of directory (not as root)
                print_directory_structure(
                    entry.path,
                    file,
                    indent + ("    " if is_last else "│   "),
                    False,
                    include_dirs,
                    depth + 1,
                )
            else:
                # Handle files
                print(
                    f'{indent}{"└──" if is_last else "├──"} 📄 {entry.name}', file=file
                )
    except PermissionError:
        print(f"{indent}└── ⚠️ Permission Denied", file=file)
    except Exception as e:
        print(f"{indent}└── ⚠️ Error: {str(e)}", file=file)


def main():
    parser = argparse.ArgumentParser(
        description="Print directory structure to terminal or file"
    )
    parser.add_argument(
        "directory",
        nargs="?",
        default=".",
        help="Target directory to scan (default: current directory)",
    )
    parser.add_argument(
        "--output", "-o", help="Output file path (if not specified, prints to terminal)"
    )
    parser.add_argument(
        "--no-prompt",
        action="store_true",
        help="Skip the prompt and show full path (default: False)",
    )
    parser.add_argument(
        "--workspace-root", help="Workspace root directory (default: current directory)"
    )
    parser.add_argument(
        "--include-dirs",
        nargs="+",
        help="List of subdirectories to include (all others will be ignored)",
    )

    args = parser.parse_args()

    # Convert to absolute path and verify directory exists
    target_dir = os.path.abspath(args.directory)
    if not os.path.isdir(target_dir):
        print(f"Error: Directory '{target_dir}' does not exist")
        sys.exit(1)

    # Set workspace root
    workspace_root = (
        os.path.abspath(args.workspace_root) if args.workspace_root else os.getcwd()
    )

    # Ask user if they want to show the full path (unless --no-prompt is used)
    show_path = True
    if not args.no_prompt:
        response = (
            input(f"Show path to '{os.path.basename(target_dir)}' directory? [Y/n]: ")
            .strip()
            .lower()
        )
        show_path = response in ["", "y", "yes"]

    try:
        if args.output:
            # Get the directory where this script is located
            script_dir = os.path.dirname(os.path.abspath(__file__))
            # Combine script directory with output filename
            output_path = os.path.join(script_dir, args.output)

            # Create output directory if it doesn't exist
            output_dir = os.path.dirname(output_path)
            if output_dir and not os.path.exists(output_dir):
                os.makedirs(output_dir)

            with open(output_path, "w", encoding="utf-8") as f:
                base_indent = ""
                if show_path:
                    base_indent = print_path_to_directory(target_dir, workspace_root, f)
                print_directory_structure(
                    target_dir, f, base_indent, include_dirs=args.include_dirs
                )
            print(f"Directory structure written to {output_path}")
        else:
            base_indent = ""
            if show_path:
                base_indent = print_path_to_directory(target_dir, workspace_root)
            print_directory_structure(
                target_dir, base_indent, include_dirs=args.include_dirs
            )
    except Exception as e:
        print(f"Error: {str(e)}")
        sys.exit(1)


if __name__ == "__main__":
    main()

# Usage examples:
# Print to terminal (default):
#   python directory_print.py
#
# Print specific directory to terminal:
#   python directory_print.py /path/to/directory
#
# Save to file in script's directory:
#   python directory_print.py /path/to/directory -o output.txt
#
# Specify workspace root:
#   python directory_print.py /path/to/directory --workspace-root /path/to/workspace
#
# Skip the prompt and always show path:
#   python directory_print.py /path/to/directory --no-prompt
