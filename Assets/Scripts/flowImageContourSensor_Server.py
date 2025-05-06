import cv2
import mediapipe as mp
import numpy as np
import socket
import json

def select_front_most_contour(contours):
    best = None
    max_score = -1
    for cnt in contours:
        area = cv2.contourArea(cnt)
        if area < 500: # 이 값이 필터링면적, 너무 낮추면 배경, 옷자락도 포함
            continue
        x, y, w, h = cv2.boundingRect(cnt)
        center_y = y + h / 2
        score = area + center_y * 2
        if score > max_score:
            max_score = score
            best = cnt
    return best

# MediaPipe selfie segmentation
mp_selfie = mp.solutions.selfie_segmentation
selfie = mp_selfie.SelfieSegmentation(model_selection=1)

cap = cv2.VideoCapture(0)

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('localhost', 9999))
server_socket.listen(1)
print("Waiting for Unity to connect...")
conn, addr = server_socket.accept()
print("Connected by", addr)

while True:
    ret, frame = cap.read()
    if not ret:
        break

    frame = cv2.flip(frame, 1)  # 좌우 반전
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    results = selfie.process(rgb)

    # 마스크에서 윤곽선 추출
    # 이 수치가 포함 정도
    mask = (results.segmentation_mask > 0.5).astype(np.uint8) * 255
    mask = cv2.medianBlur(mask, 5)
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    contour = select_front_most_contour(contours)
    points = []
    if contour is not None:
        approx = cv2.approxPolyDP(contour, 2, True) # 이 값이 클수록 단순해짐(성능)
        height = frame.shape[0]
        for pt in approx[::1]:
            x, y = pt[0]
            y_flipped = height - y  # Unity에 맞춰 Y 반전
            points.append({"x": int(x), "y": int(y_flipped)})
            cv2.circle(frame, (x, y), 3, (0, 255, 0), -1)

        cv2.drawContours(frame, [contour], -1, (0, 255, 0), 2)

        msg = json.dumps({"points": points}) + '\n'
        try:
            conn.sendall(msg.encode('utf-8'))
        except:
            break

    cv2.imshow("Filtered Contour", frame)
    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
conn.close()
server_socket.close()
cv2.destroyAllWindows()
