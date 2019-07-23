import numpy as np
import cv2
import logging


def binary_search_y(mask):
    x, y, _ = np.where(mask)
    scales = [x.max() - x.min(), y.max() - y.min()]
    index = np.argmax([scales])
    it = 0
    indexes = np.array([x, y])
    max_, min_ = indexes[index].max(), indexes[index].min()
    cut = (max_ + min_)/2
    side1, side2 = 100, 0
    while (np.abs(side1 - side2) >= 5) and it < 1000 and (max_-min_) > 5:
        side1, side2 = (indexes[index] < cut).sum(), (indexes[index] >= cut).sum()
        if side1 > side2:
            max_ = cut
        elif side2 > side1:
            min_ = cut
        cut = (max_ + min_)/2
    cut = int(cut)
    other_values = np.where(mask[:, cut, 0])[0] if index == 1 else np.where(mask[cut, :, 0])[0]
    return cut, index, other_values.min(), other_values.max()


def convert_input(image_str):
    nparr = np.fromstring(image_str, np.uint8)
    # decode image
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    return img

def resize_image(image):
    scale_percent = 20 # percent of original size
    width = int(image.shape[1] * scale_percent / 100)
    height = int(image.shape[0] * scale_percent / 100)
    dim = (width, height)
    # resize image
    resized = cv2.resize(image, dim, interpolation = cv2.INTER_AREA)
    return resized


def cut_image(request):
    #image = cv2.imread(image_name, 3Z
    image = convert_input(request.files['file'].stream.read())
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    image = resize_image(image)
    hsv = cv2.cvtColor(image, cv2.COLOR_RGB2HSV)
    golden_yellow = np.array([5, 125, 75])
    golden_brown = np.array([20, 255, 255])
    mask = cv2.inRange(hsv, golden_yellow, golden_brown)
    contours ,_ = cv2.findContours(mask, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    areas = []
    for c in contours:
        areas.append(cv2.contourArea(c))

    # Sort array of areas by size
    sorted_areas= sorted(zip(areas, contours), key=lambda x: x[0], reverse=True)
    sorted_contours = [c for _, c in sorted_areas]

    mask = np.zeros_like(image) # Create mask where white is what we want, black otherwise
    cv2.drawContours(mask, sorted_contours, 0, 255, -1) # Draw filled contour in mask
    middle, index, min_, max_ = binary_search_y(mask)
    if index == 0:
        coord1, coord2 = ((min_, int(middle)), (max_, int(middle)))
    else:
        coord1, coord2 = ((int(middle), min_), (int(middle), max_))
    cv2.line(image, coord1, coord2, (0, 0, 255), 2)
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
    _, image_encoded = cv2.imencode('.jpg', image)
    return image_encoded.tostring()