.PHONY: validate web package publish

validate:
	./scripts/validate.sh

web:
	python3 scripts/build_web_single.py

package:
	./scripts/package_source.sh

publish:
	./scripts/publish_github.sh
