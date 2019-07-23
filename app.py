from flask import Flask, request
from Server.helpers import cut_image

app = Flask(__name__)


@app.route('/', methods=['POST'])
def service():
    return cut_image(request)
