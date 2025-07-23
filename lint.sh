#!/bin/bash

# Install dotnet-format if not already installed
if ! command -v dotnet-format &> /dev/null; then
    echo "Installing dotnet-format..."
    dotnet tool install -g dotnet-format
fi

# Define solution path
SOLUTION_PATH="./CornerShop.sln"

# Check if solution file exists
if [ ! -f "$SOLUTION_PATH" ]; then
    echo "Error: Solution file not found at $SOLUTION_PATH"
    exit 1
fi

# Restore packages
echo "Restoring packages..."
dotnet restore "$SOLUTION_PATH"

if [ $? -ne 0 ]; then
    echo "Error: Package restore failed"
    exit 1
fi

# Check for formatting issues
echo "Checking for formatting issues..."
dotnet format --verify-no-changes --no-restore "$SOLUTION_PATH"

if [ $? -eq 0 ]; then
    echo "No formatting issues found!"
    exit 0
fi

# Apply formatting
echo "Applying formatting corrections..."
dotnet format --no-restore "$SOLUTION_PATH"

# Verify changes
echo "Verifying changes..."
dotnet format --verify-no-changes --no-restore "$SOLUTION_PATH"

if [ $? -eq 0 ]; then
    echo "Formatting applied successfully!"
    exit 0
else
    echo "Some formatting issues could not be automatically fixed."
    echo "Please check the output above for details."
    exit 1
fi 