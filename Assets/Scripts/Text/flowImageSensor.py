import cv2

cap = cv2.VideoCapture(0)

# 움직임 기반 + 형태 유지 강화
fgbg = cv2.createBackgroundSubtractorMOG2(
    history=1200,         # 이전보다 더 오래 기억
    varThreshold=50,      # 민감도 상승
    detectShadows=False
)

while True:
    ret, frame = cap.read()
    if not ret:
        break

    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    fgmask = fgbg.apply(gray)

    # 약한 foreground도 유지하도록 threshold 완화
    _, thresh = cv2.threshold(fgmask, 200, 255, cv2.THRESH_BINARY)
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
    thresh = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, kernel, iterations=1)
    thresh = cv2.dilate(thresh, kernel, iterations=2)

    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    for cnt in contours:
        if cv2.contourArea(cnt) > 1500:
            cv2.drawContours(frame, [cnt], -1, (0, 255, 0), 2)

    cv2.imshow("Frame", frame)
    cv2.imshow("Foreground Mask", thresh)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
