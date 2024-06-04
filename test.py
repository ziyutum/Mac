
import paho.mqtt.publish as publish
import time
import json
import random
import string
# 
mqtt_host = "localhost"
mqtt_port = 1883

# 
topic = "customer_topic"
message = "Hello, Mosquitto!++++++++++++++++++++++++++++++++++++++++"

# generate the data list randomly, which contains time s
def generate_data_list():
    data_list = [(time.strftime("%Y-%m-%d %H:%M:%S"), random.uniform(0, 100), random.uniform(0, 360)) for _ in range(5)]
    return data_list

# 
data_list = generate_data_list()
json_data = json.dumps(data_list)


# 
publish.single(topic, message, hostname=mqtt_host, port=mqtt_port)

print("Published message:", message)

publish.single(topic, json_data, hostname=mqtt_host, port=mqtt_port)

print("Published data:", json_data)
#In MQTT, messages are typically sent as byte payloads, and Python data structures like dictionaries and lists cannot be directly transmitted to an MQTT broker. Therefore, to transmit complex data structures such as lists or dictionaries in MQTT, they need to be converted into string format, often using JSON.

#JSON is a lightweight data interchange format that is easy to understand and parse, and it has good compatibility across various programming languages. By converting data into JSON format, it ensures that they can be sent to an MQTT broker and correctly parsed on the receiving end.

#In the provided code, the data list (datalist) is converted into a JSON string so that it can be published to the specified MQTT topic, and it can be correctly parsed on the receiving end.