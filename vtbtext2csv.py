import re
import csv
import sys


def process_file(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as file:
        lines = file.readlines()

    entries = []
    entry = []
    date_pattern = r'^\d{2}\.\d{2}\.\d{4}[\t]'

    for line in lines:
        if line.startswith("Проведена"):
            continue
        if re.match(date_pattern, line):
            if entry:
                entries.append(entry)
            entry = [line.strip()]
        elif entry:
            entry.append(line.strip())

    if entry:
        entries.append(entry)

    processed_entries = []

    for entry in entries:
        operation_date = ''
        amount = ''
        description = ''

        first_line_parts = entry[0].split("\t")
        operation_date = first_line_parts[0]
        amount = first_line_parts[2].replace(",", "")

        second_line_parts = entry[1].split("\t")
        description = first_line_parts[-1] + ' ' + second_line_parts[-1] + ' '

        # Join the rest as description
        description = description + ''.join([x for x in entry[2:] if x.find("\t") == -1])

        processed_entries.append([operation_date, amount, description])

    with open(output_file, 'w', newline='', encoding='utf-8') as csvfile:
        csv_writer = csv.writer(csvfile, delimiter=';')
        csv_writer.writerow(['operation_date', 'amount', 'description'])
        csv_writer.writerows(processed_entries)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python vtbtext2csv.py <input_txt_file>")
        sys.exit(1)

    input_txt_file = sys.argv[1]
    output_csv_file = input_txt_file.replace('.txt', '.csv')
    process_file(input_txt_file, output_csv_file)
