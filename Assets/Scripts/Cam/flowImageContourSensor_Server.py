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
        if area < 500:
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

    frame = cv2.flip(frame, 1)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    results = selfie.process(rgb)

    mask = (results.segmentation_mask > 0.5).astype(np.uint8) * 255
    mask = cv2.medianBlur(mask, 5)

    # 윤곽선 추출
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    contour = select_front_most_contour(contours)
    points = []

    if contour is not None:
        # 내부를 채운 마스크 생성
        closed_mask = np.zeros_like(mask)
        cv2.fillPoly(closed_mask, [contour], 255)

        # 닫힌 마스크에서 다시 윤곽선 추출
        contours, _ = cv2.findContours(closed_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        contour = select_front_most_contour(contours)

        if contour is not None:
            approx = cv2.approxPolyDP(contour, 1, True)
            height = frame.shape[0]

            # 원래 contour 점들
            for pt in approx[::1]:
                x, y = pt[0]
                y_flipped = height - y
                points.append({"x": int(x), "y": int(y_flipped)})
                cv2.circle(frame, (x, y), 3, (0, 255, 0), -1)

            # 아래 테두리 닫기 + Unity 전송용 점 추가
            leftmost = tuple(approx[approx[:, :, 0].argmin()][0])
            rightmost = tuple(approx[approx[:, :, 0].argmax()][0])
            cv2.line(frame, leftmost, rightmost, (255, 0, 0), 2)

            x1, x2 = int(leftmost[0]), int(rightmost[0])
            y_avg = int((leftmost[1] + rightmost[1]) / 2)
            y_flipped = height - y_avg
            for i in range(x1, x2 + 1, 10):
                points.append({"x": i, "y": y_flipped})

            # 전체 contour 시각화
            cv2.drawContours(frame, [contour], -1, (0, 255, 0), 2)

            # Unity로 전송
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
