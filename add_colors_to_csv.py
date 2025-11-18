#!/usr/bin/env python3
"""
Add Color column to item_placements CSV files based on ProductId.
Generates consistent colors using hash-based algorithm matching visualizer.js
"""

import csv
import os
import sys

def generate_color_for_product(product_id):
    """Generate a hex color for a ProductId using the same algorithm as visualizer.js"""
    # Hash the product_id
    hash_value = 0
    str_id = str(product_id)
    for char in str_id:
        hash_value = ord(char) + ((hash_value << 5) - hash_value)

    # Generate HSL values with good saturation and brightness
    h = abs(hash_value % 360)
    s = 65 + (abs(hash_value >> 8) % 20)  # 65-85%
    l = 55 + (abs(hash_value >> 16) % 15)  # 55-70%

    # Convert HSL to RGB
    c = (1 - abs(2 * l / 100 - 1)) * s / 100
    x = c * (1 - abs((h / 60) % 2 - 1))
    m = l / 100 - c / 2

    if h < 60:
        r, g, b = c, x, 0
    elif h < 120:
        r, g, b = x, c, 0
    elif h < 180:
        r, g, b = 0, c, x
    elif h < 240:
        r, g, b = 0, x, c
    elif h < 300:
        r, g, b = x, 0, c
    else:
        r, g, b = c, 0, x

    r = int((r + m) * 255)
    g = int((g + m) * 255)
    b = int((b + m) * 255)

    return f"#{r:02X}{g:02X}{b:02X}"

def add_colors_to_csv(input_file, output_file=None):
    """Add Color column to CSV file"""
    if output_file is None:
        output_file = input_file

    # Read the CSV
    with open(input_file, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        rows = list(reader)
        fieldnames = reader.fieldnames

    # Check if Color column already exists
    if 'Color' in fieldnames:
        print(f"Color column already exists in {input_file}")
        return

    # Add Color to fieldnames
    fieldnames = list(fieldnames) + ['Color']

    # Generate colors for each product
    product_colors = {}
    for row in rows:
        product_id = row['ProductId']
        if product_id not in product_colors:
            product_colors[product_id] = generate_color_for_product(product_id)
        row['Color'] = product_colors[product_id]

    # Write back to file
    with open(output_file, 'w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    print(f"[OK] Added colors to {output_file}")
    print(f"  - {len(rows)} items")
    print(f"  - {len(product_colors)} unique ProductIds")
    for pid, color in sorted(product_colors.items()):
        print(f"    ProductId {pid}: {color}")

def main():
    # Process all item_placements files in Results folder
    results_dir = 'Results'

    if not os.path.exists(results_dir):
        print(f"Error: {results_dir} folder not found!")
        sys.exit(1)

    files = [f for f in os.listdir(results_dir)
             if f.startswith('item_placements_') and f.endswith('.csv')]

    if not files:
        print(f"No item_placements_*.csv files found in {results_dir}/")
        sys.exit(1)

    print(f"Found {len(files)} file(s) to process:\n")

    for filename in files:
        filepath = os.path.join(results_dir, filename)
        print(f"Processing: {filename}")
        add_colors_to_csv(filepath)
        print()

if __name__ == '__main__':
    main()
