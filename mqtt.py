
## DeprecationWarning: Callback API version 1 is deprecated, update to latest version  client = mqtt.Client()
## this code needs newer version of paho mqtt. in mac, the version is 2.1.0
# functions will get error
# ----------------------------------------------------------------------------
# use the test version
##############################################################################################
import time
import json
import random
import string
import paho.mqtt.client as mqtt

# define MQTT Broker address  and port. In the mosquitto conj, the default port is 1883, if this is changed 
#we must change the listener port number in the conj datei.
broker_address = "localhost"
port = 1883

# define the MQTT topic in order to subscribe later
topic = "custom_topic"

# generate the data list randomly, which contains time s
def generate_data_list():
    data_list = [(time.strftime("%Y-%m-%d %H:%M:%S"), random.uniform(0, 100), random.uniform(0, 360)) for _ in range(5)]
    return data_list

# 
data_list = generate_data_list()

# 
for data in data_list:
    print(data)

# MQTT connect function which needs to be recalled
def on_connect(client, userdata, flags, rc):
    if rc ==0:
        print("Connected successfully with result code "+str(rc))
    else:
        print("Connected failed ")

# Create MQTT client
client = mqtt.Client(protocol=mqtt.MQTTv5)


# set the connection
client.on_connect = on_connect


# connect to the MQTT Broker
client.connect(broker_address, port, 60)

# transfer the string datalist to JSON form
string_list = generate_data_list()
json_string = json.dumps(string_list)
print(json_string)
# publish the message
client.publish(topic, json_string)

