/**
 * 图像压缩工具函数：通过 Canvas 调整尺寸并降低质量
 * @param file 原始图像文件对象
 * @param maxWidth 压缩后的最大宽度
 * @param maxHeight 压缩后的最大高度
 * @param quality 压缩质量 (0 到 1)
 * @returns Promise<string> 返回压缩后的 Base64 字符串
 */
export async function compressAvatar(
    file: File,
    config: { maxWidth: number, maxHeight: number, quality: number } = { maxWidth: 200, maxHeight: 200, quality: 0.7 }
): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = (event) => {
            const img = new Image();
            img.src = event.target?.result as string;
            img.onload = () => {
                const canvas = document.createElement('canvas');
                let width = img.width;
                let height = img.height;

                // 保持宽高比进行缩放
                if (width > height) {
                    if (width > config.maxWidth) {
                        height *= config.maxWidth / width;
                        width = config.maxWidth;
                    }
                } else {
                    if (height > config.maxHeight) {
                        width *= config.maxHeight / height;
                        height = config.maxHeight;
                    }
                }

                canvas.width = width;
                canvas.height = height;
                const ctx = canvas.getContext('2d');
                if (!ctx) {
                    reject(new Error('Failed to get canvas context'));
                    return;
                }

                ctx.drawImage(img, 0, 0, width, height);
                // 以指定质量导出为 JPEG 格式的 Base64 字符串
                resolve(canvas.toDataURL('image/jpeg', config.quality));
            };
            img.onerror = (e) => reject(e);
        };
        reader.onerror = (e) => reject(e);
    });
}
