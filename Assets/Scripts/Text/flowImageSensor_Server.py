import socket
import cv2
import json

# 캠 열기
cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

# 배경 제거기 설정
fgbg = cv2.createBackgroundSubtractorMOG2(history=1200, varThreshold=50, detectShadows=False)

# 소켓 서버 설정
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

    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    fgmask = fgbg.apply(gray)

    # 이진화 및 노이즈 제거
    _, thresh = cv2.threshold(fgmask, 200, 255, cv2.THRESH_BINARY)
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
    thresh = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, kernel, iterations=1)
    thresh = cv2.dilate(thresh, kernel, iterations=2)

    # 윤곽선 검출
    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    points = []
    height = frame.shape[0]
    for cnt in contours:
        if cv2.contourArea(cnt) > 1000:
            for pt in cnt[::15]:  # 일부만 전송 (속도 최적화)
                x, y = pt[0]
                y_flipped = height - y
                points.append({"x": int(x), "y": int(y_flipped)})
                cv2.circle(frame, (x, y), 2, (0, 255, 0), -1)  # 점 찍기

    # Unity로 전송
    msg = json.dumps({"points": points}) + '\n'
    try:
        conn.sendall(msg.encode('utf-8'))
    except:
        break

    # 현재 화면 출력
    cv2.imshow("Camera View", frame)
    cv2.imshow("Foreground Mask", thresh)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
conn.close()
server_socket.close()
cv2.destroyAllWindows()
