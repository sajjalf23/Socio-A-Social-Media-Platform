import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

export let errorRate = new Rate('errors');

export let options = {
    vus: 50,
    duration: '1m',
};

const BASE_URL = 'https://localhost:5001'; // your local app URL
const USERNAME = 'fasih@gmail.com';
const PASSWORD = 'Socio@123';

export default function () {
    // 1. Login via form
    const loginRes = http.post(
        `${BASE_URL}/Identity/Account/Login`,
        {
            Input_Email: USERNAME,
            Input_Password: PASSWORD,
            Input_RememberMe: 'false'
        },
        {
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            redirects: 0, // don't follow redirect automatically
        }
    );

    // Check login success via 302 redirect
    const loginSuccess = check(loginRes, {
        'login successful': (r) => r.status === 302
    });

    if (!loginSuccess) {
        errorRate.add(1);
        return;
    }

    // Grab the auth cookie
    const cookies = loginRes.cookies;

    // 2. Edit a post
    const postId = 1012; // replace with your post ID
    const formData = {
        postId: postId,
        caption: 'Edited via k6'
        // To add an image file:
        // imagefile: http.file(open('path/to/image.jpg', 'image/jpeg'))
    };

    const editRes = http.post(
        `${BASE_URL}/Post/Edit`,
        formData,
        {
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            cookies: cookies
        }
    );

    check(editRes, {
        'post edited successfully': (r) => r.status === 200 || r.status === 302
    });

    sleep(1);
}
