FROM ossindex/devaudit-onbuild:latest
ENV DOCKER=1
ENTRYPOINT [ "./devaudit"]
