import random
from datetime import datetime

# Generate random data list and print
def generate_and_print_data_list():
    # Header
    print("Time\t\t\tSpeed\tAngle")
    
    # Generate random data list
    data_list = [(datetime.now().strftime("%Y-%m-%d %H:%M:%S"), random.uniform(0, 100), random.uniform(0, 360)) for _ in range(5)]
    
    # Print data list
    for data in data_list:
        print(f"{data[0]}\t{data[1]}\t{data[2]}")

# Call function to generate and print data list
generate_and_print_data_list()
